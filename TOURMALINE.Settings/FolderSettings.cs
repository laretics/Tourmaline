using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TOURMALINE.Common;

namespace TOURMALINE.Settings
{
    /*
    public class FolderSettings : SettingsBase
    {
        public readonly Dictionary<string, string> Folders;

        public FolderSettings(IEnumerable<string> options)
            : base(SettingsStore.GetSettingStore(UserSettings.SettingsFilePath, UserSettings.RegistryKey, "Folders"))
        {
            Folders = new Dictionary<string, string>();
            Load(options);
        }

        public override object GetDefaultValue(string name)
        {
            return "";
        }

        protected override object GetValue(string name)
        {
            return Folders[name];
        }

        protected override void SetValue(string name, object value)
        {
            if ((string)value != "")
                Folders[name] = (string)value;
            else if (Folders.ContainsKey(name))
                Folders.Remove(name);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var name in SettingStore.GetUserNames())
                Load(optionsDictionary, name, typeof(string));
        }

        public override void Save()
        {
            foreach (var name in SettingStore.GetUserNames())
                if (!Folders.ContainsKey(name))
                    Reset(name);
            foreach (var name in Folders.Keys)
                Save(name);
        }

        public override void Save(string name)
        {
            Save(name, typeof(string));
        }

        public override void Reset()
        {
            foreach (var name in Folders.Keys)
                Reset(name);
        }
    }
    */
}
