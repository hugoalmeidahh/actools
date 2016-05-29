using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AcTools.AcdFile;
using AcTools.Utils.Helpers;
using AcTools.Windows;
using JetBrains.Annotations;

namespace AcTools.DataFile {
    public class IniFileSection : Dictionary<string, string> {
        public new dynamic this[string key] {
            get { return ContainsKey(key) ? base[key] : null; }
            set {
                if (value == null) {
                    Set(key, (string)null);
                } else {
                    Set(key, value);
                }
            }
        }

        [CanBeNull]
        public string Get(string key) {
            return ContainsKey(key) ? base[key] : null;
        }

        /// <summary>
        /// Warning! Throws exception if value is missing!
        /// </summary>
        [Obsolete]
        public bool GetBool(string key) {
            if (!ContainsKey(key)) throw new Exception("Value is missing!");
            var value = Get(key);
            return value == "1";
        }

        public bool GetBool(string key, bool defaultValue) {
            if (!ContainsKey(key)) return defaultValue;
            var value = Get(key);
            return value == "1";
        }

        public bool? GetBoolNullable(string key) {
            if (!ContainsKey(key)) return null;
            var value = Get(key);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "y", StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetStrings(string key) {
            return Get(key)?.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0) ?? new string[0];
        }
        
        public double[] GetVector3(string key) {
            var result = GetStrings(key).Select(x => FlexibleParser.ParseDouble(x, 0d)).ToArray();
            return result.Length == 3 ? result : new double[3];
        }

        /// <summary>
        /// Warning! Throws exception if value is missing!
        /// </summary>
        [Obsolete]
        public double GetDouble(string key) {
            return FlexibleParser.ParseDouble(Get(key));
        }

        public double? GetDoubleNullable(string key) {
            return FlexibleParser.TryParseDouble(Get(key));
        }

        public double GetDouble(string key, double defaultValue) {
            return FlexibleParser.ParseDouble(Get(key), defaultValue);
        }

        /// <summary>
        /// Warning! Throws exception if value is missing!
        /// </summary>
        [Obsolete]
        public int GetInt(string key) {
            return FlexibleParser.ParseInt(Get(key));
        }

        public int? GetIntNullable(string key) {
            return FlexibleParser.TryParseInt(Get(key));
        }

        public int GetInt(string key, int defaultValue) {
            return FlexibleParser.TryParseInt(Get(key)) ?? defaultValue;
        }

        /// <summary>
        /// Warning! Throws exception if value is missing!
        /// </summary>
        [Obsolete]
        public long GetLong(string key) {
            return FlexibleParser.ParseLong(Get(key));
        }

        public long? GetLongNullable(string key) {
            return FlexibleParser.TryParseLong(Get(key));
        }

        public long GetLong(string key, int defaultValue) {
            return FlexibleParser.TryParseLong(Get(key)) ?? defaultValue;
        }

        /// <summary>
        /// Warning! Throws exception if value is missing!
        /// </summary>
        [Obsolete]
        public T GetEnum<T>(string key, bool ignoreCase = true) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            var value = Get(key);
            return (T)Enum.Parse(typeof(T), value ?? "", ignoreCase);
        }

        public T? GetEnumNullable<T>(string key, bool ignoreCase = true) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            T result;
            return Enum.TryParse(Get(key), ignoreCase, out result) ? result : (T?)null;
        }

        public T GetEnum<T>(string key, T defaultValue, bool ignoreCase = true) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            T result;
            return Enum.TryParse(Get(key), ignoreCase, out result) ? result : defaultValue;
        }

        private static void Set(string key, object value) {
            throw new Exception($"Type is not supported: {value?.GetType().ToString() ?? "null"} (key: �{key}�)");
        }

        public void Set(string key, string value) {
            if (value == null) return;
            base[key] = value;
        }

        public void Set<T>(string key, IEnumerable<T> value) {
            if (value == null) return;
            Set(key, value.Select(x => x.ToInvariantString()).JoinToString(','));
        }

        public void SetId(string key, string value) {
            if (value == null) return;
            base[key] = value.ToLowerInvariant();
        }

        public void Set(string key, Enum value) {
            base[key] = Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, int value) {
            base[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, int? value) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, long value) {
            base[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, long? value) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, double value) {
            base[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, double? value) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, double value, string format) {
            base[key] = value.ToString(format, CultureInfo.InvariantCulture);
        }

        public void Set(string key, double? value, string format) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(format, CultureInfo.InvariantCulture);
        }

        public void Set(string key, bool value) {
            base[key] = value ? "1" : "0";
        }

        public void Set(string key, bool? value) {
            if (!value.HasValue) return;
            base[key] = value.Value ? "1" : "0";
        }
    }

    public class IniFile : AbstractDataFile, IEnumerable<KeyValuePair<string, IniFileSection>> {
        public IniFile(string carDir, string filename, Acd loadedAcd) : base(carDir, filename, loadedAcd) { }
        public IniFile(string carDir, string filename) : base(carDir, filename) { }
        public IniFile(string filename) : base(filename) { }
        public IniFile() { }

        public readonly Dictionary<string, IniFileSection> Content = new Dictionary<string, IniFileSection>();

        public Dictionary<string, IniFileSection>.KeyCollection Keys => Content.Keys;

        public Dictionary<string, IniFileSection>.ValueCollection Values => Content.Values;

        public IEnumerator<KeyValuePair<string, IniFileSection>> GetEnumerator() {
            return Content.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IniFileSection this[string key] {
            get {
                if (!Content.ContainsKey(key)) {
                    Content[key] = new IniFileSection();
                }
                return Content[key];
            }

            set { Content[key] = value; }
        }

        private static Regex _splitRegex;

        protected override void ParseString(string data) {
            Clear();

            if (_splitRegex == null) {
                _splitRegex = new Regex(@"\r?\n|\r|(?=\[[A-Z])", RegexOptions.Compiled);
            }

            IniFileSection currentSection = null;
            foreach (var line in _splitRegex.Split(data).Select(x => {
                var i = x.IndexOf(';');
                var j = x.IndexOf("//", StringComparison.Ordinal);
                if (j != -1 && (i == -1 || i > j)) {
                    i = j;
                }
                return i < 0 ? x.Trim() : x.Substring(0, i).Trim();
            }).Where(x => x.Length > 0)) {
                if (line[0] == '[' && line[line.Length - 1] == ']') {
                    this[line.Substring(1, line.Length - 2)] = currentSection = new IniFileSection();
                } else if (currentSection != null) {
                    var at = line.IndexOf('=');

                    if (at < 1) continue;

                    var invalidName = false;
                    for (var i = 0; i < at; i++) {
                        var c = line[i];
                        if (!(c >= 'A' && c <= 'Z' || c == '_' || c >= '0' && c <= '9')) {
                            invalidName = true;
                        }
                    }

                    if (invalidName) continue;

                    currentSection[line.Substring(0, at)] = line.Substring(at + 1);
                }
            }
        }

        public static IniFile Parse(string text) {
            var result = new IniFile();
            result.ParseString(text);
            return result;
        }

        public override void Clear() {
            Content.Clear();
        }

        public override string Stringify() {
            return Content.Select(x => (from pair in x.Value
                                        select pair.Key + "=" + pair.Value
                    ).Prepend("[" + x.Key + "]").JoinToString("\n")).JoinToString("\n\n");
        }

        public override string ToString() {
            return Stringify();
        }

        public bool ContainsKey(string key) {
            return Content.ContainsKey(key);
        }

        public void Remove(string key) {
            Content.Remove(key);
        }

        public bool IsEmptyOrDamaged() {
            return Content.Count == 0;
        }

        public static void Write(string path, string section, string key, string value) {
            Kernel32.WritePrivateProfileString(section, key, value, path);
        }

        public static void Write(string path, string section, string key, int value) {
            Kernel32.WritePrivateProfileString(section, key, value.ToString(), path);
        }

        public static void Write(string path, string section, string key, bool value) {
            Kernel32.WritePrivateProfileString(section, key, value ? "1" : "0", path);
        }

        public static void Write(string path, string section, string key, bool? value) {
            if (!value.HasValue) return;
            Kernel32.WritePrivateProfileString(section, key, value.Value ? "1" : "0", path);
        }

        [Obsolete]
        public static string Read(string path, string section, string key) {
            var rightSection = false;
            foreach (var line in File.ReadAllLines(path).Select(x => {
                var i = x.IndexOf(';');
                return i < 0 ? x.Trim() : x.Substring(0, i).Trim();
            }).Where(x => x.Length > 0)) {
                if (line[0] == '[' && line[line.Length - 1] == ']') {
                    rightSection = line.IndexOf(section, StringComparison.Ordinal) == 1 && line.Length == section.Length + 2;
                } else if (rightSection && line.StartsWith(key)) {
                    var at = line.IndexOf('=');
                    if (at >= 0 && at == key.Length) {
                        return line.Substring(at + 1);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Remove all sections by prefix like SECTION_0, SECTION_1, �
        /// </summary>
        /// <param name="prefixName">Prefix</param>
        /// <param name="startFrom">ID of first section</param>
        public void RemoveSections(string prefixName, int startFrom = 0) {
            foreach (var key in LinqExtension.RangeFrom(startFrom).Select(x => $"{prefixName}_{x}").TakeWhile(ContainsKey)) {
                Remove(key);
            }
        }

        /// <summary>
        /// Get all sections by prefix like SECTION_0, SECTION_1, �
        /// </summary>
        /// <param name="prefixName">Prefix (e.g. �SECTION�)</param>
        /// <param name="startFrom">ID of first section</param>
        public IEnumerable<IniFileSection> GetSections(string prefixName, int startFrom = 0) {
            return LinqExtension.RangeFrom(startFrom).Select(x => $"{prefixName}_{x}").TakeWhile(ContainsKey).Select(key => this[key]);
        }
    }
}