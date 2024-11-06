using System.Reflection;

namespace Render.Utilities;

public static class DemoHelper
{
	private const string Blob = ".blob";
	private const string Attachments = "Attachments";

	private const string LocalDataDirName = "localonlydata.cblite2";
	private const string RenderDataDirName = "render.cblite2";
	private const string AudioDataDirName = "renderaudio.cblite2";

	private const string BaseNamespace = "Render.Demo.demo_database.";
	private const string LocalDataNamespace = $"{BaseNamespace}{LocalDataDirName}";
	private const string RenderDataNamespace = $"{BaseNamespace}{RenderDataDirName}";
	private const string AudioDataNamespace = $"{BaseNamespace}{AudioDataDirName}";

	public static bool IsDatabaseInitialized { get; private set; }

	public static void InitializeDatabase(string targetPath)
	{
		if (Directory.EnumerateFileSystemEntries(targetPath).Any())
		{
			IsDatabaseInitialized = true;

			return;
		}

		var localDirPath = $"{targetPath}\\{LocalDataDirName}";
		var renderDirPath = $"{targetPath}\\{RenderDataDirName}";
		var audioDirPath = $"{targetPath}\\{AudioDataDirName}";
		var attachementsDirPath = $"{audioDirPath}\\{Attachments}";

		if (TryCreateDatabaseDirectories(
			localDirPath,
			renderDirPath,
			audioDirPath,
			attachementsDirPath) is false)
		{
			return;
		}

		var assembly = Assembly.GetExecutingAssembly();
		var dbResourcePaths = assembly
			.GetManifestResourceNames()
			.Where(name => name.StartsWith(BaseNamespace));

		foreach (var dbResourcePath in dbResourcePaths)
		{
			var targetParams = dbResourcePath switch
			{
				var _ when dbResourcePath.Contains(LocalDataDirName) => new { DirPath = localDirPath, NameSpace = LocalDataNamespace},
				var _ when dbResourcePath.Contains(RenderDataDirName) => new { DirPath = renderDirPath, NameSpace = RenderDataNamespace},
				var _ when dbResourcePath.Contains(AudioDataDirName) => new { DirPath = audioDirPath, NameSpace = AudioDataNamespace},
				_ => new { DirPath = string.Empty, NameSpace = string.Empty }
			};

			if (string.IsNullOrEmpty(targetParams.DirPath) || 
				string.IsNullOrEmpty(targetParams.NameSpace))
			{
				return;
			}

			if (TryWriteDatabaseResource(
				sourceAssembly: assembly,
				destinationDirPath: targetParams.DirPath,
				nameSpace: targetParams.NameSpace,
				fullResourceName: dbResourcePath) is false)
			{
				return;
			}
		}

		IsDatabaseInitialized = true;
	}

	private static bool TryCreateDatabaseDirectories(params string[] diretoryPaths)
	{
		foreach (var directory in diretoryPaths)
		{
			try
			{
				Directory.CreateDirectory(directory);
			}
			catch (Exception)
			{
				return false;
			}
		}

		return true;
	}

	private static bool TryWriteDatabaseResource(
		Assembly sourceAssembly,
		string destinationDirPath,
		string nameSpace,
		string fullResourceName)
	{
		var fileExtension = Path.GetExtension(fullResourceName);
		if (fileExtension is Blob)
		{
			destinationDirPath = $"{destinationDirPath}\\{Attachments}";
			nameSpace = $"{nameSpace}.{Attachments}";
		}

		var fileName = fullResourceName.Substring(nameSpace.Length + 1);
		var resourceStream = (Stream)null;
		var fileStream = (FileStream)null;

		try
		{
			resourceStream = sourceAssembly.GetManifestResourceStream(fullResourceName);
			if (resourceStream == null)
			{
				return false;
			}

			var fullFilePath = $"{destinationDirPath}\\{fileName}";
			fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write);

			resourceStream.CopyTo(fileStream);

			return true;
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			fileStream?.Dispose();
			resourceStream?.Dispose();
		}
	}
}