using EndFaceDetection.LogModule;
using System.IO;
using System.Text.Json;

namespace EndFaceDetection.Services.Common
{
    public class JsonHelper
    {
        /// <summary>
        /// 读取 JSON 文件并反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T? ReadJsonFile<T>(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(jsonContent);
                }
                else
                {
                    return default;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"Error reading JSON file: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 向指定的文件中写入单个内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool WriteJsonFile<T>(string filePath, T obj)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                File.WriteAllText(filePath, jsonContent);
                return true;
            }
            catch (Exception ex)
            {
                 LoggerHelper._.Error($"Error writing JSON file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 向指定的文件中写入单个内容，如果存在则追加到文件后
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GrowJsonFile<T>(string filePath, T obj)
        {
            try
            {
                bool fileExists = File.Exists(filePath);
                List<T> content;
                if (fileExists)
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        using (var jsonDocument = JsonDocument.Parse(fileStream))
                        {
                            var root = jsonDocument.RootElement;
                            content = JsonSerializer.Deserialize<List<T>>(root.GetRawText()) ?? new List<T>();
                        }
                    }
                }
                else
                {
                    content = new List<T>();
                }
                content.Add(obj);
                WriteLargeJsonFile(filePath,content);
                return true;
            }
            catch (Exception ex)
            {
                 LoggerHelper._.Error($"Error writing JSON file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 向指定的文件中写入多个内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool WriteLargeJsonFile<T>(string filePath, IEnumerable<T> data)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    using (var jsonWriter = new Utf8JsonWriter(stream))
                    {
                        jsonWriter.WriteStartArray();
                        foreach (var item in data)
                        {
                            JsonSerializer.Serialize(jsonWriter, item, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        }
                        jsonWriter.WriteEndArray();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                 LoggerHelper._.Error($"Error writing JSON file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 向指定的json文件中写入多个内容，如果json文件存在则追加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool AppendToJsonFile<T>(string filePath, IEnumerable<T> data)
        {
            try
            {
                bool fileExists = File.Exists(filePath);
                List<T> content;
                if (fileExists)
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        using (var jsonDocument = JsonDocument.Parse(fileStream))
                        {
                            var root = jsonDocument.RootElement;
                            content = JsonSerializer.Deserialize<List<T>>(root.GetRawText()) ?? new List<T>();
                        }
                    }
                }
                else
                {
                    content = new List<T>();
                }
                content.AddRange(data);
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    using (var jsonWriter = new Utf8JsonWriter(stream))
                    {
                        jsonWriter.WriteStartArray();
                        foreach (var item in content)
                        {
                            JsonSerializer.Serialize(jsonWriter, item, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        }
                        jsonWriter.WriteEndArray();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                 LoggerHelper._.Error($"Error writing JSON file: {ex.Message}");
                return false;
            }
        }

    }
}
