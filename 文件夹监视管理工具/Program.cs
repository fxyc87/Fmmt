
var version = "文件夹监控管理工具 V1.0 2024-12-19";
Console.Title = version;
Console.WriteLine(version);
Console.WriteLine();
Environment.CurrentDirectory=AppDomain.CurrentDomain.BaseDirectory;
var cfg_file = "config.txt";
if(File.Exists(cfg_file) ==false)
{
	Console.WriteLine($"未能找到配置文件:{cfg_file}，按回车键重新生成配置。");
	Console.ReadKey();
	File.WriteAllLines(cfg_file,new string[]{
		"path=d:\\ftp",
		"max_size=10"
	});
}
var cfg = File.ReadAllLines(cfg_file).Select(n =>
{
	var t = n.Split('=');
	if (t.Length != 2)
		return new KeyValuePair<string, string>();
	else
		return new KeyValuePair<string, string>(t[0], t[1]);
}).Where(n => n.Key != null).ToDictionary<string, string>();
var path = @"d:\ftp";
if(cfg.TryGetValue("path",out var _path))
	path=_path;
Console.WriteLine($"监控目录:{path}");
if (Directory.Exists(path) == false) 
{
	Console.WriteLine($"运行错误，监控目录不存在:{path}");
	Console.ReadKey();
	return;
}
long max_size = 10;
if (cfg.TryGetValue("max_size", out var _max_size))
	if(long.TryParse(_max_size,out var _max_size2))
		max_size = _max_size2;
var drive_info = new DriveInfo(Directory.GetDirectoryRoot(path));
Console.WriteLine($"分区容量：{drive_info.TotalSize / 1024.0 / 1024.0 / 1024.0:0.000}GB，分区可用容量：{drive_info.TotalFreeSpace / 1024.0 / 1024.0 / 1024.0:0.000}GB");
Console.WriteLine($"临时目录容量上限:{max_size}GB");
Console.WriteLine();
//获取目录文件列表，及文件大小，删除超过容量的旧文件
while (true) {
	Console.WriteLine("=".PadRight(10, '=')+DateTime.Now);
	var files = GetFileList(path);
	Console.WriteLine($"临时目录文件列表:{files.Count}个");
	var fi = new List<(string files, long size, DateTime create)>();
    foreach (var item in files)
	{
		var _fi = new FileInfo(item);
		fi.Add((_fi.FullName, _fi.Length, _fi.CreationTime));
	}
	var size = fi.Sum(n => n.size);
	Console.WriteLine($"临时目录文件容量:{size / 1024.0 / 1024.0 / 1024.0:0.000}GB");
	long max_size_byte = max_size * 1024 * 1024 * 1024;
	if (size > max_size_byte)
	{
		var fi2 = fi.OrderBy(n => n.create).ToList();
		var del_files = new List<string>();
		long del_size = 0;
		for (int i = 0; i < fi2.Count; i++)
		{
			if (del_size < size - max_size_byte)
			{
				del_files.Add(fi2[i].files);
				del_size += fi2[i].size;
			}
			else
				break;
		}
		Console.WriteLine($"超过限制容量{max_size}GB，即将删除{del_files.Count}个旧文件，合计{del_size / 1024 / 1024:0.000}MB");
		Thread.Sleep(TimeSpan.FromSeconds(30));
		//删除文件
		foreach (var f in del_files) {
			Console.WriteLine($"删除文件：{f}");
			try
			{
				File.Delete(f);
			}
			catch (Exception e) { 
				Console.WriteLine($"删除文件失败 {f}，{e.Message}");
			}
			Thread.Sleep(TimeSpan.FromSeconds(0.5));
		}
	}
	else {
		Console.WriteLine($"未超过限制容量{max_size}GB，1分钟后再次检查。");
	}
	Thread.Sleep(TimeSpan.FromMinutes(1));
}



/// <summary>
/// 获取目录下的所有文件 含子目录中的文件  不用递归调用
/// </summary>
List<string> GetFileList(string path)
{
	Stack<string> dirs = new Stack<string>();
	List<string> files = new List<string>();
	dirs.Push(path);
	while (dirs.Count > 0)
	{
		var cur_dir = dirs.Pop();
		files.AddRange(Directory.EnumerateFiles(cur_dir));
		foreach (var item in Directory.EnumerateDirectories(cur_dir))
			dirs.Push(item);
	}
	return files;
}


