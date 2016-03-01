#pragma warning disable 169

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ChromeAppCreator.Annotations;
using Newtonsoft.Json;

namespace ChromeAppCreator.Logic
{
    class IconEntryListConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var col = value as ObservableCollection<IconEntry>;
            var dict = col.ToDictionary(item => item.Size.ToString(), item => item.File);
            serializer.Serialize(writer, dict);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, string>>(reader);
            return new ObservableCollection<IconEntry>(dict.Select(item => new IconEntry(int.Parse(item.Key), item.Value)).ToList());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (ObservableCollection<IconEntry>);
        }
    }

    public class IconEntry
    {
        public IconEntry(int size, string file)
        {
            Size = size;
            File = file;
        }

        public int Size { get; set; }
        public string File { get; set; }
    }

    public class Manifest
    {
        public Manifest()
        {
            WebUrl = "";
        }

        [JsonProperty("manifest_version", Required = Required.Always)]
        private readonly int _manifestVersion = 2;

        [JsonProperty("name", Required = Required.Always)]
        public string Name = "";

        [JsonProperty("description"), DefaultValue("")]
        public string Description = "";

        [JsonProperty("version", Required = Required.Always), DefaultValue("1.0")]
        public string Version = "";

        [JsonProperty("icons"), JsonConverter(typeof(IconEntryListConverter))]
        public ObservableCollection<IconEntry> Icons = new ObservableCollection<IconEntry>();

        [JsonProperty("app", Required = Required.Always)]
        private App _app;

        [JsonIgnore]
        public string WebUrl
        {
            get { return _app.Launch.WebUrl; }
            set { _app.Launch.WebUrl = value; }
        }
    }

    internal struct Launch
    {
        [JsonProperty("web_url", Required = Required.Always)]
        public string WebUrl;
    }
    
    internal struct App
    {
        [JsonProperty("launch", Required = Required.Always)]
        public Launch Launch;
    }
}
