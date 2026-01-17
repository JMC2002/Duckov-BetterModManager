using Duckov.Modding.UI;
using JmcModLib.Reflection;
using JmcModLib.UI;
using JmcModLib.UI.Icon;
using JmcModLib.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterModManager.UI
{
    public static class RestartEntry
    {
        private const string RestartBtnName = "Btn_RestartGame";
        private static Sprite RestartSprite => IconGenerator.Restart;

        public static void Setup(ModManagerUI __instance)
        {
            try
            {
                // 获取 quitBtn
                var quitBtn = MemberAccessor.Get(typeof(ModManagerUI), "quitBtn")
                                            .GetValue<ModManagerUI, Button>(__instance);

                if (quitBtn == null) return;

                // 检查是否已存在 (注意：去父级下找)
                if (quitBtn.transform.parent.Find(RestartBtnName) != null) return;

                // 创建
                CreateRestartButton(quitBtn);
            }
            catch (Exception ex)
            {
                ModLogger.Error("创建重启按钮失败", ex);
            }
        }

        private static void CreateRestartButton(Button quitBtn)
        {
            // 克隆
            GameObject btnObj = UnityEngine.Object.Instantiate(quitBtn.gameObject, quitBtn.transform.parent);
            btnObj.name = RestartBtnName;
            btnObj.SetActive(true);

            Button newBtn = btnObj.GetComponent<Button>();
            RectTransform newRect = btnObj.GetComponent<RectTransform>();
            RectTransform quitRect = quitBtn.GetComponent<RectTransform>();

            // 忽略布局 (防止被拉回)
            var layoutElement = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            // 强制刷新布局以获取宽度，不然是0
            LayoutRebuilder.ForceRebuildLayoutImmediate(quitRect);

            float btnWidth = quitRect.rect.width;

            if (btnWidth < 10f)
            {
                btnWidth = 200f;
                ModLogger.Info($"[UI] 检测到宽度异常 ({quitRect.rect.width})，使用兜底宽度 200");
            }
            else
            {
                ModLogger.Info($"[UI] 读取到真实宽度: {btnWidth}");
            }

            newRect.anchorMin = quitRect.anchorMin;
            newRect.anchorMax = quitRect.anchorMax;
            newRect.pivot = quitRect.pivot;

            // 复制大小
            newRect.sizeDelta = quitRect.sizeDelta;
            newRect.localScale = quitRect.localScale;

            // 0.382 * 0.382倍宽度偏移
            float offset = btnWidth * 1.145924f;

            // 向右移动 (+)
            Vector2 newPos = quitRect.anchoredPosition + new Vector2(offset, 0);

            newRect.anchoredPosition = newPos;

            newBtn.onClick.RemoveAllListeners();
            newBtn.onClick.AddListener(() => OnRestartClicked(newBtn));

            float iconSize = 48f;

            Image bgImg = newBtn.targetGraphic as Image ?? newBtn.GetComponent<Image>();

            var allImages = newBtn.GetComponentsInChildren<Image>(true);
            bool iconReplaced = false;

            foreach (var img in allImages)
            {
                if (img != bgImg && img.gameObject != newBtn.gameObject)
                {
                    img.sprite = RestartSprite;
                    img.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
                    img.gameObject.SetActive(true);
                    iconReplaced = true;
                }
            }

            if (!iconReplaced)
            {
                GameObject iconObj = new("RestartIcon");
                iconObj.transform.SetParent(newBtn.transform, false);
                Image icon = iconObj.AddComponent<Image>();
                icon.sprite = RestartSprite;
                icon.raycastTarget = false;
                var texts = newBtn.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 0) icon.color = texts[0].color;
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, iconSize);
            }

            foreach (var txt in newBtn.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                txt.text = "";
                txt.gameObject.SetActive(false);
            }
        }

        private static void OnRestartClicked(Component context)
        {
            SimpleConfirmUI.Show(
                contextObject: context.transform,
                message: L10n.Get("是否重启游戏？"),
                onConfirm: DoReboot,
                styleTemplate: context,
                confirmText: L10n.Get("重启"),
                cancelText: L10n.Get("取消"),
                confirmColor: new Color(1f, 0.6f, 0f) // 橙色
            );
        }

        private static void DoReboot()
        {
            ModLogger.Info("正在重启游戏...");

            // 获取当前游戏 exe 的全路径
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string workingDir = System.IO.Path.GetDirectoryName(exePath);

            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string cmdPath = System.IO.Path.Combine(sys32, "cmd.exe");
            string timeoutPath = System.IO.Path.Combine(sys32, "timeout.exe");

            // 构造 CMD 命令：
            // /c : 执行完关闭
            // timeout /t 2 /nobreak > NUL : 等待 2 秒 (不显示倒计时)
            // && : 等待结束后执行
            // start "" "..." : 启动游戏
            string cmdArgs = $"/c \"\"{timeoutPath}\" /t 2 /nobreak > NUL && start \"\" /d \"{workingDir}\" \"{exePath}\"\"";

            System.Diagnostics.ProcessStartInfo startInfo = new()
            {
                FileName = cmdPath,
                Arguments = cmdArgs,
                UseShellExecute = true,
                CreateNoWindow = false, // 不显示黑色的 CMD 窗口
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDir
            };

            try
            {
                ModLogger.Info($"执行命令：{cmdPath}");
            System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"重启失败: {ex.Message}");
                // 如果连这样都失败，尝试最后的保底手段：不延时直接开
                try
                {
                    System.Diagnostics.Process.Start(exePath);
                }
                catch { }
            }

            // 立即退出当前游戏
            Application.Quit();
        }
    }
}