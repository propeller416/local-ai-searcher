using System;
using System.IO;

namespace Application.Helpers;

public static class AppPaths
{
    public static string GetAppRootDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        
        // На Mac, если приложение запущено из .app пакета
        // baseDir будет выглядеть как: /path/to/LocalAiSearcher.app/Contents/MacOS/
        if (OperatingSystem.IsMacOS() && baseDir.Contains(".app/Contents/MacOS"))
        {
            // Поднимаемся на 3 уровня вверх, чтобы получить папку, в которой лежит .app
            var appBundlePath = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
            if (appBundlePath != null)
            {
                var parentDir = Directory.GetParent(appBundlePath)?.FullName;
                if (parentDir != null)
                {
                    return parentDir;
                }
            }
        }

        // Для Windows или запуска из IDE папка будет рядом с исполняемым файлом
        return baseDir;
    }

    public static string GetAppDataDirectory() => Path.Combine(GetAppRootDirectory(), "LocalAiSearcherData");

    public static string GetDatabasePath() => Path.Combine(GetAppDataDirectory(), "local_ai_searcher.db");
    public static string GetDocumentsDirectory() => Path.Combine(GetAppDataDirectory(), "documents");
    public static string GetHistoryDirectory() => Path.Combine(GetAppDataDirectory(), "history");
    public static string GetSettingsFilePath() => Path.Combine(GetAppDataDirectory(), "settings.json");
    
    // Встроенные модели (внутри .app на Mac или рядом с .exe на Windows)
    public static string GetBundledModelsDirectory() => Path.Combine(AppContext.BaseDirectory, "models");
    
    // Пользовательские модели (в папке LocalAiSearcherData)
    public static string GetUserModelsDirectory() => Path.Combine(GetAppDataDirectory(), "models");
    
    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetAppDataDirectory());
        Directory.CreateDirectory(GetDocumentsDirectory());
        Directory.CreateDirectory(GetHistoryDirectory());
        Directory.CreateDirectory(GetBundledModelsDirectory());
        Directory.CreateDirectory(GetUserModelsDirectory());
    }
    
    public static string ResolveModelPath(string modelFileName)
    {
        var userModelPath = Path.Combine(GetUserModelsDirectory(), modelFileName);
        if (File.Exists(userModelPath))
        {
            return userModelPath;
        }
        
        var bundledModelPath = Path.Combine(GetBundledModelsDirectory(), modelFileName);
        if (File.Exists(bundledModelPath))
        {
            return bundledModelPath;
        }
        
        // По умолчанию возвращаем путь в пользовательской папке, чтобы ошибка указывала туда
        return userModelPath;
    }
}
