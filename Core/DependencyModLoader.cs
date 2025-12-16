using Duckov.Modding;
using SodaCraft.Localizations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// 项目地址：https://github.com/JMC2002/ModTemplate
namespace BetterModManager.Core
{
    // 依赖项结构体
    public struct ModDependency
    {
        public string Name;
        public ulong SteamId;
        public ModDependency(string name, ulong steamId = 0) { Name = name; SteamId = steamId; }
        public static implicit operator ModDependency(string name) => new(name);
    }

    // 如果你在多个 MOD 里用这份代码，请确保 namespace 不同，或者使用 internal
    public abstract class DependencyModLoader : Duckov.Modding.ModBehaviour
    {
        // 当前DependencyModLoader代码版本号
        public const string LOADER_VERSION = "1.0.1";

        private HashSet<string> _missingDependencies = default!;
        private bool _isLoaded = false;

        // 存储依赖项的 SteamID 映射
        private Dictionary<string, ulong> _dependencyIdMap = default!;

        // --- UI 显示控制 ---
        private bool _showUI = false;
        private string _uiTitle = "";
        private string _uiMessage = "";
        private Color _uiColor = Color.red;

        // UI 交互：点击时需要打开的 SteamID 列表
        private List<ulong> _targetSteamIdsForUI = [];

        // --- 堆叠控制 ---
        // 这是一个特殊的标识符，所有用这份代码的 MOD 都会认得它
        private const string TOKEN_NAME = "__DEP_UI_TOKEN__";
        private GameObject _myToken = default!; // 我生成的那个标记物体

        // 设置一个“耐心时间”，超过这个时间还没加载完，才显示提示
        // 建议设为 5-8 秒，足以覆盖大多数慢速加载的情况
        private const float PATIENCE_TIME = 5.0f;

        // 必须实现：定义依赖列表
        protected abstract ModDependency[] GetDependencies();
        // 必须实现：创建业务逻辑组件
        protected abstract MonoBehaviour CreateImplementation(ModManager master, ModInfo info);

        protected override void OnAfterSetup()
        {
            Debug.Log($"[{info.name}] Initializing DependencyLoader v{LOADER_VERSION} ...");

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var requiredDeps = GetDependencies();
            if (requiredDeps == null || requiredDeps.Length == 0)
            {
                TryInitImplementation();
                return;
            }

            // 1. 构建映射并初始化缺失列表
            _dependencyIdMap = [];
            var requiredNames = new List<string>();

            foreach (var dep in requiredDeps)
            {
                _dependencyIdMap[dep.Name] = dep.SteamId;
                requiredNames.Add(dep.Name);
            }

            _missingDependencies = [.. requiredNames];

            // 2. 物理检查：文件夹是否存在？
            var installedMods = ModManager.modInfos.Select(m => m.name).ToHashSet();
            List<string> notInstalledList = [];
            List<ulong> missingIds = []; // 收集缺失的 ID

            foreach (var req in requiredNames)
            {
                if (!installedMods.Contains(req))
                {
                    notInstalledList.Add(req);
                    // 如果有 ID，加到列表里
                    if (_dependencyIdMap.TryGetValue(req, out ulong id) && id > 0)
                    {
                        missingIds.Add(id);
                    }
                }
            }

            if (notInstalledList.Count > 0)
            {
                // 严重错误：未安装
                // 传入 missingIds，让 UI 知道点击要跳转
                ShowNotification(
                    GetLocalizedText("MISSING_TITLE"),
                    $"{GetLocalizedText("MISSING_MSG")}\n{string.Join(", ", notInstalledList)}",
                    true,
                    missingIds
                );
                return;
            }

            // 3. 内存检查
            CheckByAppDomain();

            if (_missingDependencies.Count == 0)
            {
                TryInitImplementation();
                return;
            }

            // 4. 原生配置检查 (仅用于日志和判断)
            List<string> nativelyDisabled = GetNativelyDisabledMods(_missingDependencies);

            if (nativelyDisabled.Count > 0)
            {
                Debug.LogWarning($"[{info.name}] 原生配置显示依赖未启用，进入超时检查模式: {string.Join(", ", nativelyDisabled)}");
            }
            else
            {
                Debug.Log($"[{info.name}] 正在等待依赖加载: {string.Join(", ", _missingDependencies)}");
            }

            ModManager.OnModActivated += OnModActivatedHandler;
            StartCoroutine(WaitForDependencyRoutine());
        }

        // --- 等待协程 ---
        private IEnumerator WaitForDependencyRoutine()
        {
            float timer = 0f;
            bool warningShown = false;

            while (!_isLoaded)
            {
                CheckByAppDomain();

                if (_missingDependencies.Count == 0)
                {
                    if (warningShown) CloseNotification();
                    TryInitImplementation();
                    yield break;
                }

                timer += Time.deltaTime;

                if (timer > PATIENCE_TIME && !warningShown)
                {
                    warningShown = true;
                    // 等待超时：这是“未启用”或“太慢”，不需要跳转 Steam
                    ShowNotification(
                        GetLocalizedText("WAITING_TITLE"),
                        $"{GetLocalizedText("WAITING_MSG")}\n{string.Join(", ", _missingDependencies)}",
                        false,
                        null // 不需要跳转
                    );
                }

                yield return null;
            }
        }

        private void CheckByAppDomain()
        {
            // 获取当前内存里所有的程序集名字
            var loadedAsms = AppDomain.CurrentDomain.GetAssemblies()
                                .Select(a => a.GetName().Name)
                                .ToHashSet();

            // 如果依赖项出现在内存里，直接移除，视为已解决
            _missingDependencies.RemoveWhere(loadedAsms.Contains);
        }

        private List<string> GetNativelyDisabledMods(HashSet<string> targets)
        {
            var list = new List<string>();
            var checkMethod = typeof(ModManager).GetMethod("ShouldActivateMod", BindingFlags.Instance | BindingFlags.NonPublic);
            if (checkMethod == null) return list;

            foreach (var req in targets)
            {
                // 本地和创意工坊可能有多个同名 ModInfo，任一启用即可
                var matchingMods = ModManager.modInfos.Where(m => m.name == req).ToList();
                if (matchingMods.Count == 0) continue;

                bool isAnyEnabled = false;
                foreach (var modInfo in matchingMods)
                {
                    try
                    {
                        if ((bool)checkMethod.Invoke(ModManager.Instance, new object[] { modInfo }))
                        { isAnyEnabled = true; break; }
                    }
                    catch { }
                }
                if (!isAnyEnabled) list.Add(req);
            }
            return list;
        }

        private void CheckAlreadyLoaded()
        {
            var activeMods = ModManager.GetCurrentActiveModList();
            if (activeMods != null)
            {
                _missingDependencies.RemoveWhere(dep => activeMods.Contains(dep));
            }
        }

        private void OnModActivatedHandler(ModInfo activatedModInfo, Duckov.Modding.ModBehaviour modBehaviour)
        {
            if (_isLoaded) return;

            if (_missingDependencies.Contains(activatedModInfo.name))
            {
                _missingDependencies.Remove(activatedModInfo.name);

                if (_missingDependencies.Count == 0)
                {
                    ModManager.OnModActivated -= OnModActivatedHandler;
                    TryInitImplementation();
                }
            }
        }

        private void TryInitImplementation()
        {
            if (_isLoaded) return; // 如果有严重错误UI，也可以选择继续加载或者不加载，通常如果没报错就可以加载

            // 成功加载后关闭可能存在的通知UI
            // 即使有黄色警告UI，只要依赖齐了，也允许加载
            CloseNotification();


            Debug.Log($"[{info.name}] 依赖就绪，启动业务逻辑。");
            // (CreateImplementation 内部会 AddComponent，此时前置库肯定在内存里)
            CreateImplementation(this.master, this.info);
            _isLoaded = true;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requestedName = new AssemblyName(args.Name).Name;
            var dependencies = GetDependencies();

            // 如果请求的是我们的依赖项
            if (dependencies.Contains(requestedName))
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == requestedName)
                    {
                        return asm; // 强行返回内存里已有的那份
                    }
                }
            }
            return null!;
        }

        // --- UI 方法 ---
        private void ShowNotification(string title, string msg, bool isFatal, List<ulong>? steamIds = null)
        {
            _showUI = true;
            _uiTitle = $"[{info.displayName}] {title}";
            _uiMessage = msg;
            // 红色表示严重错误(缺文件)，黄色表示警告(没勾选)
            // _uiColor = isFatal ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.8f, 0.0f, 1f);
            _uiColor = isFatal
                               ? new Color(0.9f, 0.2f, 0.2f, 1f)  // 红色
                               : new Color(0.9f, 0.5f, 0.0f, 1f); // 深橙色 (适配白字)

            // 保存需要跳转的 ID
            _targetSteamIdsForUI = steamIds ?? [];

            // 生成一个看不见的 Token，作为我们“正在显示UI”的信标
            if (_myToken == null)
            {
                _myToken = new GameObject(TOKEN_NAME);
                _myToken.transform.SetParent(this.transform); // 挂在自己下面
            }

            if (isFatal) Debug.LogError($"[{info.name}] {title}: {msg}");
            else Debug.LogWarning($"[{info.name}] {title}: {msg}");
        }

        private void CloseNotification()
        {
            _showUI = false;
            if (_myToken != null)
            {
                Destroy(_myToken);
                _myToken = null!;
            }

            // 如果是因为缺失前置而报错，关闭 UI 后我们依然处于“未加载”状态
            // 除非玩家后来启用了前置触发了事件，否则这里什么都不做
        }

        // --- 计算排队位置 ---
        private int CalculateStackIndex()
        {
            // 如果没有 ModManager，就默认排第 0
            if (ModManager.Instance == null) return 0;

            // 1. 获取所有挂在 ModManager 下的 MOD
            // 注意：我们通过遍历 ModManager 的子物体来查找，这是性能最高的做法
            var siblings = ModManager.Instance.transform;
            var activeTokens = new List<string>();

            foreach (Transform child in siblings)
            {
                // 检查这个 MOD 是否有一个叫 TOKEN_NAME 的子物体
                // Find 是浅层搜索子物体，正好符合需求
                if (child.Find(TOKEN_NAME) != null)
                {
                    // 我们用 MOD 的名字来作为排序依据
                    // 只要每个 MOD 的 GameObject 名字是唯一的（Duckov 确实如此），这就是稳定的
                    activeTokens.Add(child.name);
                }
            }

            // 2. 排序
            // 确保大家的顺序一致，不会乱跳
            activeTokens.Sort();

            // 3. 找我在里面的位置
            // this.name 就是当前 MOD GameObject 的名字
            return activeTokens.IndexOf(this.name);
        }

        // --- 核心修改 1: 检测 Steam 是否在运行 ---
        private bool IsSteamRunning()
        {
            try
            {
                // 反射检查 SteamAPI.IsSteamRunning()
                // 这样不需要引用 Steamworks.NET.dll 也能编译
                Type steamApiType = Type.GetType("Steamworks.SteamAPI, com.rlabrecque.steamworks.net") ?? AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "Steamworks.SteamAPI");
                if (steamApiType != null)
                {
                    MethodInfo isRunningMethod = steamApiType.GetMethod("IsSteamRunning", BindingFlags.Public | BindingFlags.Static);
                    if (isRunningMethod != null)
                    {
                        return (bool)isRunningMethod.Invoke(null, null);
                    }
                }
            }
            catch
            {
                // 忽略异常，默认认为没运行
            }
            return false;
        }

        // 通过内部浏览器根据单个 SteamID 打开创意工坊页面
        private void OpenWorkshopPage(ulong id)
        {
            string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={id}";
            bool success = false;

            try
            {
                // 查找 SteamFriends 类型
                Type steamFriendsType = Type.GetType("Steamworks.SteamFriends, com.rlabrecque.steamworks.net") ?? AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "Steamworks.SteamFriends");
                if (steamFriendsType != null)
                {
                    // 获取方法信息
                    MethodInfo activateOverlayMethod = steamFriendsType.GetMethod("ActivateGameOverlayToWebPage", BindingFlags.Public | BindingFlags.Static);

                    if (activateOverlayMethod != null)
                    {
                        // Steamworks.NET 不同版本该方法的参数数量不同（有的1个，有的2个）
                        var parameters = activateOverlayMethod.GetParameters();

                        if (parameters.Length == 1)
                        {
                            // 旧版本：只接受 URL
                            activateOverlayMethod.Invoke(null, [url]);
                        }
                        else if (parameters.Length == 2)
                        {
                            // 新版本：接受 URL + Mode
                            // 第二个参数是 Enum，传 int 0 对应 k_EActivateGameOverlayToWebPageMode_Default
                            activateOverlayMethod.Invoke(null, [url, 0]);
                        }

                        success = true;
                        Debug.Log($"[{info.name}] 已通过 Steam Overlay 打开: {id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{info.name}] 无法呼出 Steam Overlay，回退到系统浏览器。原因: {ex.Message}");
            }

            // 回退机制
            if (!success)
            {
                Debug.Log($"[{info.name}] 通过系统默认浏览器打开订阅页面: {id}");
                Application.OpenURL(url);
            }
        }

        // --- 批量打开 ---
        private void OpenWorkshopPage(List<ulong> ids)
        {
            if (ids == null || ids.Count == 0) return;

            // 检查是否是 Steam 环境
            bool isSteam = IsSteamRunning();

            if (!isSteam)
            {
                // 非 Steam 玩家：全部用 HTTP 打开外部浏览器
                foreach (var id in ids)
                {
                    Application.OpenURL($"https://steamcommunity.com/sharedfiles/filedetails/?id={id}");
                }
            }
            else if (ids.Count == 1)
            {
                // 当只缺一个前置，直接用 Steam 打开
                Application.OpenURL($"steam://url/CommunityFilePage/{ids[0]}");
            }
            else // 否则在内部浏览器中打开，因为 Steam 协议只能打开一个
            {
                foreach (var id in ids)
                {
                    OpenWorkshopPage(id);
                }
            }
        }

        private void OnGUI()
        {
            if (!_showUI || _isLoaded || this == null) return;

            GUI.depth = -9999;

            // 分辨率适配
            float referenceHeight = 1080f;
            float scale = Screen.height / referenceHeight;
            scale = Mathf.Max(scale, 0.8f);

            // 增加基础高度，给文字换行留出空间
            float width = 420f * scale;
            float height = 140f * scale;
            float margin = 20f * scale;
            float spacing = 10f * scale;

            // 动态计算位置
            int stackIndex = CalculateStackIndex();
            if (stackIndex < 0) stackIndex = 0;
            float stackOffset = stackIndex * (height + spacing);

            Rect boxRect = new(
                Screen.width - width - margin,
                Screen.height - margin - height - stackOffset,
                width,
                height
            );

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _uiColor;

            // --- 点击背景 ---
            if (GUI.Button(boxRect, ""))
            {
                // 如果有 SteamID，点击背景是打开浏览器
                if (_targetSteamIdsForUI != null && _targetSteamIdsForUI.Count > 0)
                {
                    OpenWorkshopPage(_targetSteamIdsForUI);
                }

                CloseNotification();
            }

            Rect contentRect = new(
                boxRect.x + (15 * scale),
                boxRect.y + (8 * scale),
                boxRect.width - (30 * scale),
                boxRect.height - (16 * scale)
            );

            // --- 样式定义 (清除默认边距) ---
            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(18 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            titleStyle.normal.textColor = Color.white;
            // 清除默认间距，防止占用额外高度
            titleStyle.margin = new RectOffset(0, 0, 0, 0);
            titleStyle.padding = new RectOffset(0, 0, 0, 0);

            GUIStyle msgStyle = new(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15 * scale),
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            msgStyle.normal.textColor = Color.white;
            // 清除默认间距
            msgStyle.margin = new RectOffset(0, 0, 4, 4); // 上下给一点点间距即可

            GUIStyle tipStyle = new(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight
            };
            tipStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            // 清除默认间距
            tipStyle.margin = new RectOffset(0, 0, 0, 0);
            tipStyle.padding = new RectOffset(0, 0, 0, 0);

            // --- 绘制 ---
            GUILayout.BeginArea(contentRect);

            GUILayout.Label(_uiTitle, titleStyle);

            // 使用固定的 Space 代替 FlexibleSpace 的一部分功能，防止挤得太紧
            GUILayout.Space(5 * scale);

            // 消息本体
            GUILayout.Label(_uiMessage, msgStyle);

            // 自动撑开，把关闭按钮推到底部
            GUILayout.FlexibleSpace();

            // --- 动态提示文字 ---
            string tipText;
            if (_targetSteamIdsForUI != null && _targetSteamIdsForUI.Count > 0)
            {
                // 如果有链接，显示“点击订阅”
                tipText = GetLocalizedText("SUBSCRIBE_BTN");
            }
            else
            {
                // 否则显示“点击关闭”
                tipText = GetLocalizedText("CLOSE_BTN");
            }
            GUILayout.Label(tipText, tipStyle);

            GUILayout.EndArea();

            GUI.backgroundColor = originalColor;
        }
        // --- 轻量级本地化方法 ---
        private string GetLocalizedText(string key)
        {
            SystemLanguage lang = LocalizationManager.CurrentLanguage;

            return key switch
            {
                // Key 1: API 错误标题
                "API_ERR_TITLE" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "API 错误",
                    SystemLanguage.ChineseTraditional => "API 錯誤",
                    SystemLanguage.French => "Erreur API",
                    SystemLanguage.German => "API-Fehler",
                    SystemLanguage.Japanese => "APIエラー",
                    SystemLanguage.Korean => "API 오류",
                    SystemLanguage.Portuguese => "Erro de API",
                    SystemLanguage.Russian => "Ошибка API",
                    SystemLanguage.Spanish => "Error de API",
                    _ => "API Error" // Default English
                },

                // Key 2: API 错误内容
                "API_ERR_MSG" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "无法访问 ModManager.ShouldActivateMod，请联系作者。",
                    SystemLanguage.ChineseTraditional => "無法訪問 ModManager.ShouldActivateMod，請聯繫作者。",
                    SystemLanguage.French => "Impossible d'accéder à ModManager.ShouldActivateMod, contactez l'auteur.",
                    SystemLanguage.German => "Zugriff auf ModManager.ShouldActivateMod nicht möglich, bitte Autor kontaktieren.",
                    SystemLanguage.Japanese => "ModManager.ShouldActivateModにアクセスできません。作者に連絡してください。",
                    SystemLanguage.Korean => "ModManager.ShouldActivateMod에 액세스할 수 없습니다. 작성자에게 문의하세요.",
                    SystemLanguage.Portuguese => "Não é possível acessar ModManager.ShouldActivateMod, contate o autor.",
                    SystemLanguage.Russian => "Невозможно получить доступ к ModManager.ShouldActivateMod, свяжитесь с автором.",
                    SystemLanguage.Spanish => "No se puede acceder a ModManager.ShouldActivateMod, contacte al autor.",
                    _ => "Cannot access ModManager.ShouldActivateMod, please contact the author."
                },

                // Key 3: 缺失前置标题
                "MISSING_TITLE" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "缺失前置 (未安装)",
                    SystemLanguage.ChineseTraditional => "缺失前置 (未安裝)",
                    SystemLanguage.French => "Dépendance Manquante",
                    SystemLanguage.German => "Fehlende Abhängigkeit",
                    SystemLanguage.Japanese => "前提MOD不足 (未インストール)",
                    SystemLanguage.Korean => "선행 MOD 누락 (미설치)",
                    SystemLanguage.Portuguese => "Dependência Ausente",
                    SystemLanguage.Russian => "Отсутствует зависимость",
                    SystemLanguage.Spanish => "Dependencia Faltante",
                    _ => "Missing Dependency"
                },

                // Key 4: 缺失前置内容前缀
                "MISSING_MSG" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "请订阅以下 MOD:",
                    SystemLanguage.ChineseTraditional => "請訂閱以下 MOD:",
                    SystemLanguage.French => "Veuillez vous abonner aux MODs suivants :",
                    SystemLanguage.German => "Bitte abonnieren Sie folgende MODs:",
                    SystemLanguage.Japanese => "次のMODを購読してください:",
                    SystemLanguage.Korean => "다음 MOD를 구독하십시오:",
                    SystemLanguage.Portuguese => "Por favor, inscreva-se nos seguintes MODs:",
                    SystemLanguage.Russian => "Пожалуйста, подпишитесь на следующие моды:",
                    SystemLanguage.Spanish => "Por favor suscríbase a los siguientes MODs:",
                    _ => "Please subscribe to the following MODs:"
                },

                // Key 5: 未启用标题
                "DISABLED_TITLE" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "前置未启用",
                    SystemLanguage.ChineseTraditional => "前置未啟用",
                    SystemLanguage.French => "Dépendance Désactivée",
                    SystemLanguage.German => "Abhängigkeit Deaktiviert",
                    SystemLanguage.Japanese => "前提MOD無効",
                    SystemLanguage.Korean => "선행 MOD 비활성화됨",
                    SystemLanguage.Portuguese => "Dependência Desativada",
                    SystemLanguage.Russian => "Зависимость Отключена",
                    SystemLanguage.Spanish => "Dependencia Desactivada",
                    _ => "Dependency Disabled"
                },

                // Key 6: 未启用内容前缀
                "DISABLED_MSG" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "前置库被禁用，请在 MOD 列表中勾选:",
                    SystemLanguage.ChineseTraditional => "前置庫被禁用，請在 MOD 列表中勾選:",
                    SystemLanguage.French => "Bibliothèque désactivée, veuillez cocher dans la liste :",
                    SystemLanguage.German => "Bibliothek deaktiviert, bitte in der Liste aktivieren:",
                    SystemLanguage.Japanese => "前提ライブラリが無効です。リストで有効にしてください:",
                    SystemLanguage.Korean => "라이브러리가 비활성화되었습니다. 목록에서 확인하십시오:",
                    SystemLanguage.Portuguese => "Biblioteca desativada, verifique na lista de MODs:",
                    SystemLanguage.Russian => "Библиотека отключена, пожалуйста, отметьте в списке:",
                    SystemLanguage.Spanish => "Biblioteca desactivada, marque en la lista de MODs:",
                    _ => "Dependency library is disabled, please check it in the MOD list:"
                },

                // Key 7: 关闭按钮
                "CLOSE_BTN" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "[ 点击关闭 ]",
                    SystemLanguage.ChineseTraditional => "[ 點擊關閉 ]",
                    SystemLanguage.French => "[ Fermer ]",
                    SystemLanguage.German => "[ Schließen ]",
                    SystemLanguage.Japanese => "[ 閉じる ]",
                    SystemLanguage.Korean => "[ 닫기 ]",
                    SystemLanguage.Portuguese => "[ Fechar ]",
                    SystemLanguage.Russian => "[ Закрыть ]",
                    SystemLanguage.Spanish => "[ Cerrar ]",
                    _ => "[ Click to Close ]"
                },

                // Key 8: 等待提示标题
                "WAITING_TITLE" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "正在等待前置...",
                    SystemLanguage.ChineseTraditional => "正在等待前置...",
                    SystemLanguage.French => "En attente de dépendance...",
                    SystemLanguage.German => "Warten auf Abhängigkeit...",
                    SystemLanguage.Japanese => "前提MOD待機中...",
                    SystemLanguage.Korean => "선행 MOD 대기 중...",
                    SystemLanguage.Portuguese => "Aguardando Dependência...",
                    SystemLanguage.Russian => "Ожидание зависимости...",
                    SystemLanguage.Spanish => "Esperando Dependencia...",
                    _ => "Waiting for Dependency..."
                },

                // Key 9: 等待提示内容
                "WAITING_MSG" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "加载时间较长，或前置库未启用。\n请耐心等待，或检查 MOD 列表是否勾选:",
                    SystemLanguage.ChineseTraditional => "加載時間較長，或前置庫未啟用。\n請耐心等待，或檢查 MOD 列表是否勾選:",
                    SystemLanguage.French => "Le chargement est plus long que prévu ou la dépendance est désactivée.\nVeuillez patienter ou vérifier si elle est activée :",
                    SystemLanguage.German => "Laden dauert länger als erwartet oder Abhängigkeit ist deaktiviert.\nBitte warten oder prüfen, ob aktiviert:",
                    SystemLanguage.Japanese => "読み込みに時間がかかっているか、無効化されています。\n待機するか、有効化されているか確認してください:",
                    SystemLanguage.Korean => "로딩이 지연되거나 선행 MOD가 비활성화되었습니다.\n잠시 기다리거나 활성화 여부를 확인하십시오:",
                    SystemLanguage.Portuguese => "O carregamento demora mais que o esperado ou a dependência está desativada.\nAguarde ou verifique se está ativada:",
                    SystemLanguage.Russian => "Загрузка длится дольше обычного или зависимость отключена.\nПодождите или проверьте, включена ли она:",
                    SystemLanguage.Spanish => "La carga tarda más de lo esperado o la dependencia está desactivada.\nEspere o verifique si está activada:",
                    _ => "Loading takes longer than expected, or dependency is disabled.\nPlease wait, or check if enabled:"
                },

                // Key 10: 订阅按钮文本
                "SUBSCRIBE_BTN" => lang switch
                {
                    SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "[ 点击订阅 ]",
                    SystemLanguage.ChineseTraditional => "[ 點擊訂閱 ]",
                    SystemLanguage.French => "[ S'abonner ]",
                    SystemLanguage.German => "[ Abonnieren ]",
                    SystemLanguage.Japanese => "[ 購読する ]",
                    SystemLanguage.Korean => "[ 구독하기 ]",
                    SystemLanguage.Portuguese => "[ Inscrever-se ]",
                    SystemLanguage.Russian => "[ Подписаться ]",
                    SystemLanguage.Spanish => "[ Suscribirse ]",
                    _ => "[ Click to Subscribe ]"
                },

                _ => key // Fallback: return key itself if not found
            };
        }

        protected override void OnBeforeDeactivate()
        {
            // 确保清理干净
            CloseNotification();

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            ModManager.OnModActivated -= OnModActivatedHandler;
            if (_isLoaded) this.gameObject.SendMessage("ManualDeactivate", SendMessageOptions.DontRequireReceiver);
        }
    }
}