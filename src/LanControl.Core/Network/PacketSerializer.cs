using System;
using System.Web.Script.Serialization;

namespace LanControl.Core.Network
{
    public static class PacketSerializer
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue,
            RecursionLimit = 100
        };

        public static string Serialize<T>(T obj)
        {
            if (obj == null) return string.Empty;
            return Serializer.Serialize(obj);
        }

        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            return Serializer.Deserialize<T>(json);
        }
    }
}
