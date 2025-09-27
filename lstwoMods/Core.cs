using UnityEngine;
using UniverseLib.UI;
using UniverseLib;
using UniverseLib.Config;
using lstwoMODS_Core.UI;
using System.Collections.Generic;
using lstwoMODS_Core.UI.TabMenus;
using lstwoMODS_Core.Hacks;
using System.Reflection;
using System.Collections;
using System;
using lstwoMODS_Core.UI.Keybinds;
using lstwoMODS_Core.Keybinds;
using System.Linq;
using System.IO;
using MelonLoader;

[assembly: MelonInfo(typeof(lstwoMODS_Core.Core), "lstwoMods", "1.0.0", "cheesydev", null)]
[assembly: MelonGame()]
namespace lstwoMODS_Core {
    public class Core : MelonMod {
        public const string GUID = "net.lstwo.lstwomods_core";

        // ASSETS
        public static AssetBundle AssetBundle { get; private set; }

        // QUICK ACCESS
        public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        
        public static MelonLogger.Instance LogSource { get => Instance.LoggerInstance; }
        public static MelonPreferences_Category PrefsCategory { get; private set;}

        // INSTANCES
        public static Core Instance { get; private set; }
        public static AssetUtils AssetUtils { get; set; }

        public static UIBase UiBase { get; private set; }
        public static MainPanel MainPanel { get; private set; }
        public static KeybindPanel KeybindPanel { get; private set; }

        public static List<BaseTab> TabMenus { get; private set; } = new();
        public static List<BaseHack> Hacks { get; private set; } = new();

        // OTHER FEATURES
        public static KeybindManager KeybindManager { get; private set; }

        // UI TOGGLING
        public static Action<bool> OnUIToggle { get; set; }
        public static List<Func<bool>> UIConditions { get; set; } = new();

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            Instance = this;
            
            AssetUtils = new();
            AssetUtils.AssetBundles = new()
            {
                new("lstwoMODS_Core.Resources.assets.6000.bundle", new("6000.0.23")),
                new("lstwoMODS_Core.Resources.assets.2020.bundle", new("2020.3.28")),
                new("lstwoMODS_Core.Resources.assets.2017.bundle", new("2017.1.0")),
                new("lstwoMODS_Core.Resources.assets.5.6.bundle", new("5.6.0")),
                new("lstwoMODS_Core.Resources.assets.5.3.4.bundle", new("5.3.4")),
                new("lstwoMODS_Core.Resources.assets.5.2.5.bundle", new("5.2.5")),
            };
            
            AssetBundle = AssetUtils.LoadCompatibleAssetBundle();
            KeybindManager = new GameObject("KeybindManager").AddComponent<KeybindManager>();
            UnityEngine.Object.DontDestroyOnLoad(KeybindManager.gameObject);

            HacksUIHelper.LoadConfig();
            KeybindManager.LoadAllKeybinds();

            LoggerInstance.Msg($"Plugin {GUID} is loaded!");

            InitMods();
            InitUI();
        }

        public override void OnApplicationQuit() {
            base.OnApplicationQuit();
            PrefsCategory.SaveToFile();
            MelonPreferences.Save();
        }

        public static void InitMods()
        {
            InitChildClasses<BaseHack>();
        }

        public static void InitUI()
        {
            UniverseLibConfig config = new()
            {
                Disable_EventSystem_Override = false,
                Force_Unlock_Mouse = true
            };

            Universe.Init(1f, () =>
            {
                UiBase = UniversalUI.RegisterUI("lstwo.NotAzza", null);

                MainPanel = new(UiBase);
                KeybindPanel = new(UiBase);

                KeybindPanel.Enabled = false;
                UiBase.Enabled = false;
            }, null, config);
        }

        public static void InitChildClasses<T>()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<Type>();

            foreach(var assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error getting types from assembly '{assembly.FullName}': {ex.Message} {ex.StackTrace}");
                }
            }

            foreach (var type in types)
            {
                try
                {
                    if(type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
                    {
                        Activator.CreateInstance(type);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error evaluating / initializing type '{type.FullName}': {ex.Message} {ex.StackTrace}");
                }
            }
        }

        public static Coroutine _StartCoroutine(IEnumerator routine)
        {
            return (Coroutine)MelonCoroutines.Start(routine);
        }

        public override void OnUpdate()
        {
            if(Input.GetKeyDown(KeyCode.F2))
            {
                ToggleUI();
            }

            foreach(var hack in Hacks)
            {
                hack.Update();
            }
        }

        private void ToggleUI()
        {
            foreach (var condition in UIConditions)
            {
                if (!condition.Invoke())
                {
                    UiBase.Enabled = false;
                    return;
                }
            }

            var enabled = !UiBase.Enabled;
            UiBase.Enabled = enabled;

            if (enabled)
            {
                MainPanel.Refresh();
            }

            OnUIToggle?.Invoke(enabled);
        }
    }
}