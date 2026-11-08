using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.Data.Persistence
{
    public sealed class LocalDataWriteException : IOException
    {
        public LocalDataWriteException(Exception cause)
            : base("No se pudieron guardar los cambios locales.", cause) { }
    }

    public class JsonStorageService
    {
        private enum ReadResult { Loaded, Missing, Damaged, Unavailable }
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly object FileGate = new object();
        private static readonly Queue<string> Notices = new Queue<string>();
        private static readonly Dictionary<string, ReadResult> ReportedReadErrors = new Dictionary<string, ReadResult>();
        private readonly string filePath;
        private string BackupPath => filePath + ".bak";
        private string TemporaryPath => filePath + ".tmp";

        public string FilePath => filePath;
        public JsonStorageService()
            : this(Path.Combine(Application.persistentDataPath, AppConstants.TasksFileName)) { }

        public JsonStorageService(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo JSON es obligatoria.", nameof(filePath));

            this.filePath = Path.GetFullPath(filePath);
        }

        public void Save<T>(T data) where T : class
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            lock (FileGate)
            {
                try
                {
                    ReadResult primary = Read<T>(filePath, out _, out _);
                    // Preserve an unreadable file and any backup until recovery or explicit deletion.
                    if (primary == ReadResult.Unavailable || primary == ReadResult.Damaged ||
                        (primary == ReadResult.Missing && Read<T>(BackupPath, out _, out _) != ReadResult.Missing))
                        throw new IOException("Los datos existentes requieren revisión antes de escribir.");

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    WriteAtomic(JsonUtility.ToJson(data, true), primary == ReadResult.Loaded,
                        primary == ReadResult.Loaded ? BackupPath : null);
                    ReportedReadErrors.Remove(filePath);
                }
                catch (Exception exception) when (IsFileError(exception))
                {
                    Report($"No se guardaron los cambios de {DataName()}. Revisa el almacenamiento e inténtalo de nuevo.");
                    Debug.LogWarning($"POMO: error de escritura en {Path.GetFileName(filePath)}. {exception.Message}");
                    throw new LocalDataWriteException(exception);
                }
            }
        }

        public bool TryLoad<T>(out T data) where T : class
        {
            lock (FileGate)
            {
                ReadResult primary = Read(filePath, out data, out _);
                if (primary == ReadResult.Loaded)
                {
                    ReportedReadErrors.Remove(filePath);
                    return true;
                }

                if (primary == ReadResult.Unavailable)
                {
                    ReportReadFailure(primary, $"No se pudo acceder a {DataName()}. Revisa el almacenamiento.");
                    return false;
                }

                ReadResult backup = Read(BackupPath, out T recovered, out string json);
                if (backup == ReadResult.Loaded)
                {
                    try
                    {
                        WriteAtomic(json, primary != ReadResult.Missing, null);
                        data = recovered;
                        ReportedReadErrors.Remove(filePath);
                        Report($"Se recuperó la copia anterior de {DataName()}. Revisa si faltan cambios recientes.");
                        return true;
                    }
                    catch (Exception exception) when (IsFileError(exception))
                    {
                        ReportReadFailure(ReadResult.Unavailable,
                            $"Se encontró una copia de {DataName()}, pero no se pudo restaurar. Los archivos se conservaron.");
                        return false;
                    }
                }

                if (primary != ReadResult.Missing || backup != ReadResult.Missing)
                    ReportReadFailure(primary == ReadResult.Damaged ? primary : backup,
                        $"No se pudo recuperar {DataName()}. Los archivos se conservaron y no se sobrescribirán.");
                else
                    ReportedReadErrors.Remove(filePath);
                return false;
            }
        }

        private static ReadResult Read<T>(string path, out T data, out string json) where T : class
        {
            data = null;
            json = null;
            try
            {
                json = File.ReadAllText(path, Utf8).Trim();
                if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
                    return ReadResult.Damaged;
                data = JsonUtility.FromJson<T>(json);
                return data == null ? ReadResult.Damaged : ReadResult.Loaded;
            }
            catch (FileNotFoundException) { return ReadResult.Missing; }
            catch (DirectoryNotFoundException) { return ReadResult.Missing; }
            catch (ArgumentException) { return ReadResult.Damaged; }
            catch (Exception exception) when (IsFileError(exception)) { return ReadResult.Unavailable; }
        }

        private void WriteAtomic(string json, bool replace, string backup)
        {
            bool createdTemporary = false;
            try
            {
                using (var stream = new FileStream(TemporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    createdTemporary = true;
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (replace) File.Replace(TemporaryPath, filePath, backup);
                else File.Move(TemporaryPath, filePath);
            }
            finally
            {
                if (createdTemporary)
                {
                    try { File.Delete(TemporaryPath); }
                    catch (Exception exception) when (IsFileError(exception)) { Debug.LogWarning(exception.Message); }
                }
            }
        }

        private static bool IsFileError(Exception exception) =>
            exception is IOException || exception is UnauthorizedAccessException || exception is NotSupportedException;

        private string DataName()
        {
            switch (Path.GetFileName(filePath))
            {
                case AppConstants.TasksFileName: return "tus tareas";
                case AppConstants.WorkBlocksFileName: return "tus bloques";
                case AppConstants.ProgressFileName: return "tu progreso";
                case AppConstants.StoreInventoryFileName: return "tu inventario";
                case AppConstants.RhythmRemindersFileName: return "tus recordatorios";
                default: return "tus datos";
            }
        }

        private void ReportReadFailure(ReadResult result, string message)
        {
            if (ReportedReadErrors.TryGetValue(filePath, out ReadResult previous) && previous == result) return;
            ReportedReadErrors[filePath] = result;
            Report(message);
        }

        private static void Report(string message)
        {
            if (!Notices.Contains(message)) Notices.Enqueue(message);
        }

        public static bool TryTakeNotice(out string message)
        {
            lock (FileGate)
            {
                message = Notices.Count > 0 ? Notices.Dequeue() : null;
                return message != null;
            }
        }

        public static void ClearNotices()
        {
            lock (FileGate)
            {
                Notices.Clear();
                ReportedReadErrors.Clear();
            }
        }
    }
}
