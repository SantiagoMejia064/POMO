using System;
using System.IO;
using System.Text;
using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.Data.Persistence
{
    public class JsonStorageService
    {
        private readonly string filePath;

        public string FilePath => filePath;

        public JsonStorageService()
            : this(Path.Combine(Application.persistentDataPath, AppConstants.TasksFileName))
        {
        }

        public JsonStorageService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("La ruta del archivo JSON es obligatoria.", nameof(filePath));
            }

            this.filePath = filePath;
        }

        public void Save<T>(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(data, true);
            string temporaryPath = filePath + ".tmp";

            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Copy(temporaryPath, filePath, true);
            File.Delete(temporaryPath);
        }

        public bool TryLoad<T>(out T data) where T : class
        {
            data = null;

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException)
            {
                Debug.LogWarning($"No fue posible cargar el archivo JSON '{filePath}': {exception.Message}");
                data = null;
                return false;
            }
        }
    }
}
