using Duckov.Modding;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BetterModManager.Core
{
    // 建议设为 internal，防止不同 MOD 之间类名冲突
    // 如果你在多个 MOD 里用这份代码，请确保 namespace 不同，或者使用 internal
    public abstract class DependencyModLoader : Duckov.Modding.ModBehaviour
    {
        private HashSet<string> _missingDependencies = default!;
        private bool _isLoaded = false;

        // --- UI 显示控制 ---
        private bool _showUI = false;
        private string _uiTitle = "";
        private string _uiMessage = "";
        private Color _uiColor = Color.red;

        // --- 堆叠控制 ---
        // 这是一个特殊的标识符，所有用这份代码的 MOD 都会认得它
        private const string TOKEN_NAME = "__DEP_UI_TOKEN__";
        private GameObject _myToken = default!; // 我生成的那个标记物体

        // 必须实现：定义依赖列表
        protected abstract string[] GetDependencies();
        // 必须实现：创建业务逻辑组件
        protected abstract MonoBehaviour CreateImplementation(ModManager master, ModInfo info);

        protected override void OnAfterSetup()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var required = GetDependencies();
            if (required == null || required.Length == 0)
            {
                TryInitImplementation();
                return;
            }

            _missingDependencies = [.. required];

            // 准备反射：获取官方判断是否激活的方法
            var checkMethod = typeof(ModManager).GetMethod("ShouldActivateMod", BindingFlags.Instance | BindingFlags.NonPublic);

            if (checkMethod == null)
            {
                ShowNotification(
                    GetLocalizedText("API_ERR_TITLE"),
                    GetLocalizedText("API_ERR_MSG"),
                    true
                );
                return;
            }

            List<string> notInstalledList = []; // 完全没安装
            List<string> disabledList = [];     // 安装了但没勾选

            // 2. 遍历每一个依赖项进行检查
            foreach (var reqName in required)
            {
                // 查找所有同名 MOD (兼容本地和工坊共存的情况)
                var matchingMods = ModManager.modInfos.Where(m => m.name == reqName).ToList();

                if (matchingMods.Count == 0)
                {
                    notInstalledList.Add(reqName);
                    continue;
                }

                // 检查是否启用
                bool isAnyEnabled = false;
                foreach (var modInfo in matchingMods)
                {
                    try
                    {
                        bool isEnabled = (bool)checkMethod.Invoke(ModManager.Instance, new object[] { modInfo });
                        if (isEnabled)
                        {
                            isAnyEnabled = true;
                            break;
                        }
                    }
                    catch { /* 忽略反射异常 */ }
                }

                if (!isAnyEnabled)
                {
                    disabledList.Add(reqName);
                }
            }

            // 3. 处理结果

            // 情况 A：严重错误 - 未安装
            if (notInstalledList.Count > 0)
            {
                ShowNotification(
                    GetLocalizedText("MISSING_TITLE"),
                    $"{GetLocalizedText("MISSING_MSG")}\n{string.Join(", ", notInstalledList)}",
                    true
                );
                return; // 终止加载
            }

            // 情况 B：警告 - 未启用
            if (disabledList.Count > 0)
            {
                // 弹出一个黄色警告，告诉玩家去勾选，但不终止程序（因为玩家可能马上就去勾选了）
                ShowNotification(
                    GetLocalizedText("DISABLED_TITLE"),
                    $"{GetLocalizedText("DISABLED_MSG")}\n{string.Join(", ", disabledList)}",
                    false
                );
                Debug.LogWarning($"[{info.name}] 等待前置库启用: {string.Join(", ", disabledList)}");
            }

            // 情况 C：检查是否已经在内存里 (应对加载顺序)
            CheckAlreadyLoaded();

            if (_missingDependencies.Count == 0)
            {
                TryInitImplementation();
            }
            else
            {
                // 还有依赖没加载（可能是未启用，也可能是排在后面）
                // 订阅事件等待
                if (disabledList.Count == 0)
                {
                    Debug.Log($"[{info.name}] 正在等待加载顺序: {string.Join(", ", _missingDependencies)}");
                }
                ModManager.OnModActivated += OnModActivatedHandler;
            }
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
                    // 如果因为之前的“未启用”弹出了警告框，现在自动关闭它，因为玩家已经启用了
                    _showUI = false;

                    ModManager.OnModActivated -= OnModActivatedHandler;
                    TryInitImplementation();
                }
            }
        }

        private void TryInitImplementation()
        {
            if (_isLoaded) return; // 如果有严重错误UI，也可以选择继续加载或者不加载，通常如果没报错就可以加载

            // 即使有黄色警告UI，只要依赖齐了，也允许加载
            // (CreateImplementation 内部会 AddComponent，此时 JmcModLib 肯定在内存里)
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
        private void ShowNotification(string title, string msg, bool isFatal)
        {
            _showUI = true;
            _uiTitle = $"[{info.displayName}] {title}";
            _uiMessage = msg;
            // 红色表示严重错误(缺文件)，黄色表示警告(没勾选)
            // _uiColor = isFatal ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.8f, 0.0f, 1f);
            _uiColor = isFatal
                               ? new Color(0.9f, 0.2f, 0.2f, 1f)  // 红色
                               : new Color(0.9f, 0.5f, 0.0f, 1f); // 深橙色 (适配白字)


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

            if (GUI.Button(boxRect, ""))
            {
                CloseNotification();
            }

            Rect contentRect = new(
                boxRect.x + (15 * scale),
                boxRect.y + (8 * scale),
                boxRect.width - (30 * scale),
                boxRect.height - (16 * scale)
            );

            // --- 样式定义 (关键：清除默认边距) ---

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

            // 关闭按钮
            GUILayout.Label(GetLocalizedText("CLOSE_BTN"), tipStyle);

            GUILayout.EndArea();

            GUI.backgroundColor = originalColor;
        }
        // --- 核心新增：轻量级本地化方法 ---
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