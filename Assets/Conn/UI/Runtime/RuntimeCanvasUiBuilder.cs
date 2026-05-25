using Conn.Core.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Conn.UI.Runtime
{
    public static class RuntimeCanvasUiBuilder
    {
        public const string CanvasName = "Runtime Canvas";
        public const string EventSystemName = "EventSystem";
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static readonly string[] CommonPanelNames =
        {
            "RuntimeHud",
            "RuntimeInteractionPrompt",
            "RuntimePrimaryPanel",
            "RuntimeSecondaryPanel",
            "RuntimeBottomPanel"
        };

        public static readonly string[] TitlePanelNames =
        {
            "TitleRoot",
            "TitleButtons"
        };

        public static readonly string[] TownPanelNames =
        {
            "TownHud",
            "TownInteractionPrompt",
            "TownQuestBoardPanel",
            "TownShopPanel",
            "TownCharacterInventoryPanel",
            "TownNoticePanel"
        };

        public static readonly string[] DungeonPanelNames =
        {
            "DungeonHud",
            "DungeonInteractionPrompt",
            "DungeonReturnPanel",
            "DungeonPlacementReadout"
        };

        public static readonly string[] CombatPanelNames =
        {
            "CombatEnemyStagePanel",
            "CombatCommandPanel",
            "CombatDicePanel",
            "CombatLogPanel",
            "CombatStatusPanel"
        };

        public static readonly string[] EndingPanelNames =
        {
            "EndingResultPanel",
            "EndingButtons"
        };

        public static RuntimeCanvasUi EnsureRuntimeCanvas(GameObject owner, GameSceneId sceneId)
        {
            var ui = owner.GetComponent<RuntimeCanvasUi>();
            if (ui == null)
            {
                ui = owner.AddComponent<RuntimeCanvasUi>();
            }

            ui.SceneId = sceneId;
            var canvas = FindOrCreateCanvas();
            ConfigureCanvas(canvas);
            EnsureEventSystem();
            EnsurePanelRoots(canvas.transform, sceneId);
            ui.Bind(canvas);
            return ui;
        }

        public static Rect NormalizedSafeRectForPanel(string panelName)
        {
            if (panelName.Contains("Hud") || panelName.Contains("Title") || panelName.Contains("Ending"))
            {
                return new Rect(0.02f, 0.78f, 0.96f, 0.2f);
            }

            if (panelName.Contains("Prompt"))
            {
                return new Rect(0.32f, 0.08f, 0.36f, 0.12f);
            }

            if (panelName.Contains("Bottom") || panelName.Contains("Dice") || panelName.Contains("Log"))
            {
                return new Rect(0.02f, 0.02f, 0.96f, 0.28f);
            }

            if (panelName.Contains("Secondary") || panelName.Contains("Shop") || panelName.Contains("Inventory") || panelName.Contains("Status"))
            {
                return new Rect(0.68f, 0.22f, 0.3f, 0.54f);
            }

            return new Rect(0.02f, 0.22f, 0.3f, 0.54f);
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvasObject = GameObject.Find(CanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(CanvasName);
            }

            var canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }

            return canvas;
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject(EventSystemName);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void EnsurePanelRoots(Transform canvasTransform, GameSceneId sceneId)
        {
            for (var i = 0; i < CommonPanelNames.Length; i++)
            {
                EnsurePanel(canvasTransform, CommonPanelNames[i]);
            }

            var scenePanels = PanelsFor(sceneId);
            for (var i = 0; i < scenePanels.Length; i++)
            {
                EnsurePanel(canvasTransform, scenePanels[i]);
            }
        }

        public static string[] PanelsFor(GameSceneId sceneId)
        {
            return sceneId switch
            {
                GameSceneId.Title => TitlePanelNames,
                GameSceneId.Town => TownPanelNames,
                GameSceneId.Dungeon => DungeonPanelNames,
                GameSceneId.Combat => CombatPanelNames,
                GameSceneId.Ending => EndingPanelNames,
                _ => CommonPanelNames
            };
        }

        private static RectTransform EnsurePanel(Transform canvasTransform, string name)
        {
            var child = canvasTransform.Find(name);
            GameObject panelObject;
            if (child == null)
            {
                panelObject = new GameObject(name);
                panelObject.transform.SetParent(canvasTransform, false);
            }
            else
            {
                panelObject = child.gameObject;
            }

            var rect = panelObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = panelObject.AddComponent<RectTransform>();
            }

            ApplyNormalizedRect(rect, NormalizedSafeRectForPanel(name));

            var group = panelObject.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panelObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            return rect;
        }

        private static void ApplyNormalizedRect(RectTransform rect, Rect normalized)
        {
            rect.anchorMin = new Vector2(normalized.xMin, normalized.yMin);
            rect.anchorMax = new Vector2(normalized.xMax, normalized.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
