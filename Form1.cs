using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace _3DSExtractorPacker
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ModifyHeader(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
            byte[] pattern = { 0x4E, 0x43, 0x53, 0x44 }; // "NCSD" in hex

            int headerStart = FindPattern(data, pattern);
            if (headerStart != -1)
            {
                byte[] newData = new byte[data.Length];
                Array.Copy(data, 0, newData, 0, headerStart - 256);
                for (int i = headerStart - 256; i < headerStart; i++)
                {
                    newData[i] = 0xFF;
                }
                Array.Copy(data, headerStart, newData, headerStart, data.Length - headerStart);

                File.WriteAllBytes(filePath, newData);
            }
        }

        private int FindPattern(byte[] data, byte[] pattern)
        {
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }

        private void ExtractPartition(string filePath, string romFolder, string partitionNumber)
        {
            string partitionFolder = Path.Combine(romFolder, $"cfa{partitionNumber}");
            Directory.CreateDirectory(partitionFolder);

            string partitionCommand = $"/C 3dstool -xvt{partitionNumber}f cci \"{partitionFolder}\\{partitionNumber}.cfa\" \"{filePath}\"";
            RunCommand(partitionCommand);

            string cfaCommand = $"/C 3dstool -xvtf cfa \"{partitionFolder}\\{partitionNumber}.cfa\" --header \"{partitionFolder}\\ncchheader.bin\" --romfs \"{partitionFolder}\\romfs.bin\"";
            RunCommand(cfaCommand);

            string romfsBinPath = Path.Combine(partitionFolder, "romfs.bin");
            string romfsFolder = Path.Combine(partitionFolder, "romfs");
            string romfsCommand = $"/C 3dstool -xvtf romfs \"{romfsBinPath}\" --romfs-dir \"{romfsFolder}\"";
            RunCommand(romfsCommand);
        }

        private void ExtractExeFs(string exefsBinPath, string exhBinPath, string exefsFolder, string exefsHeaderPath)
        {
            byte[] exhData = new byte[0x10];
            using (FileStream fs = new FileStream(exhBinPath, FileMode.Open))
            {
                fs.Read(exhData, 0, 0x10);
            }
            bool useU = exhData[0x0D] == 1;

            string exefsCommand = $"/C 3dstool -{(useU ? "xvtfu" : "xvtf")} exefs \"{exefsBinPath}\" --exefs-dir \"{exefsFolder}\" --header \"{exefsHeaderPath}\"";
            RunCommand(exefsCommand);
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "3DS文件(*.3ds)|*.3ds";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    ModifyHeader(filePath);

                    string? dirPath = Path.GetDirectoryName(filePath);
                    if (dirPath == null)
                    {
                        throw new InvalidOperationException("无法确定目录路径.");
                    }
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string romFolder = Path.Combine(dirPath, fileName);
                    Directory.CreateDirectory(romFolder);

                    string cciCommand = $"/C 3dstool -xvt0f cci \"{romFolder}\\0.cxi\" \"{filePath}\" --header \"{romFolder}\\ncsdheader.bin\"";
                    string output = RunCommand(cciCommand);

                    if (output.Contains("INFO: partition 1"))
                    {
                        ExtractPartition(filePath, romFolder, "1");
                    }
                    if (output.Contains("INFO: partition 7"))
                    {
                        ExtractPartition(filePath, romFolder, "7");
                    }

                    string cxi0Folder = Path.Combine(romFolder, "cxi0");
                    Directory.CreateDirectory(cxi0Folder);

                    string cxiCommand = $"/C 3dstool -xvtf cxi \"{romFolder}\\0.cxi\" --header \"{cxi0Folder}\\ncchheader.bin\" --exh \"{cxi0Folder}\\exh.bin\" --plain \"{cxi0Folder}\\plain.bin\" --exefs \"{cxi0Folder}\\exefs.bin\" --romfs \"{cxi0Folder}\\romfs.bin\"";
                    RunCommand(cxiCommand);

                    string romfsBinPath = Path.Combine(cxi0Folder, "romfs.bin");
                    string exefsBinPath = Path.Combine(cxi0Folder, "exefs.bin");
                    string exhBinPath = Path.Combine(cxi0Folder, "exh.bin");
                    string romfsFolder = Path.Combine(cxi0Folder, "romfs");
                    string exefsFolder = Path.Combine(cxi0Folder, "exefs");
                    string exefsHeaderPath = Path.Combine(exefsFolder, "exefsheader.bin");

                    string romfsCommand = $"/C 3dstool -xvtf romfs \"{romfsBinPath}\" --romfs-dir \"{romfsFolder}\"";
                    RunCommand(romfsCommand);

                    ExtractExeFs(exefsBinPath, exhBinPath, exefsFolder, exefsHeaderPath);

                    MessageBox.Show($"提取完成{fileName}", "成功了", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private string RunCommand(string command)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = command;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            txtOutput.AppendText(output);
            if (!string.IsNullOrEmpty(error))
            {
                txtOutput.AppendText("错误: " + error);
            }

            return output;
        }

        private void PackExeFsRomFs(string folderPath)
        {
            // Process cxi0 exefs and romfs
            string cxi0ExefsPath = Path.Combine(folderPath, "cxi0", "exefs");
            string cxi0RomfsPath = Path.Combine(folderPath, "cxi0", "romfs");
            string exefsCommand = $"/C 3dstool -cvtfz exefs {folderPath}/cxi0/exefs.bin --header {folderPath}/cxi0/exefs/exefsheader.bin --exefs-dir {folderPath}/cxi0/exefs";
            RunCommand(exefsCommand);
            string romfsCommand = $"/C 3dstool -cvtf romfs {folderPath}/cxi0/romfs.bin --romfs-dir {folderPath}/cxi0/romfs";
            RunCommand(romfsCommand);

            // Process cfa1 - cfa7 exefs and romfs
            for (int i = 1; i < 8; i++)
            {
                string cfaPath = Path.Combine(folderPath, $"cfa{i}");
                if (!Directory.Exists(cfaPath))
                {
                    continue;
                }
                string cfaRomfsPath = Path.Combine(cfaPath, "romfs");
                romfsCommand = $"/C 3dstool -cvtf romfs {cfaPath}/romfs.bin --romfs-dir {cfaRomfsPath}";
                RunCommand(romfsCommand);
            }
        }

        private void PackCxiCfa(string folderPath)
        {
            // Pack cxi0 into cxi
            string cxi0Path = Path.Combine(folderPath, "cxi0");
            string cxiCommand = $"/C 3dstool -cvtf cxi {folderPath}/0.cxi --header {cxi0Path}/ncchheader.bin --exh {cxi0Path}/exh.bin --plain {cxi0Path}/plain.bin --exefs {cxi0Path}/exefs.bin --romfs {cxi0Path}/romfs.bin --key0";
            RunCommand(cxiCommand);

            // Pack cfa1 - cfa7 into cfa
            for (int i = 1; i < 8; i++)
            {
                string cfaPath = Path.Combine(folderPath, $"cfa{i}");
                if (!Directory.Exists(cfaPath))
                {
                    continue;
                }
                string cfaCommand = $"/C 3dstool -cvtf cfa {folderPath}/{i}.cfa --header {cfaPath}/ncchheader.bin --romfs {cfaPath}/romfs.bin";
                RunCommand(cfaCommand);
            }
        }

        private void PackCci(string folderPath)
        {
            string? dirPath = Path.GetDirectoryName(folderPath);
            if (dirPath == null)
            {
                throw new InvalidOperationException("无法确定目录路径.");
            }
            string fileName = Path.GetFileName(folderPath);
            string partitions = "";

            for (int i = 1; i < 8; i++)
            {
                string cfaPath = Path.Combine(folderPath, $"cfa{i}");
                if (Directory.Exists(cfaPath))
                {
                    partitions += i.ToString();
                }
            }

            string cciCommand = $"/C 3dstool -cvt0{partitions}f cci {folderPath}/0.cxi";
            for (int i = 1; i < 8; i++)
            {
                string cfaPath = Path.Combine(folderPath, $"cfa{i}");
                if (Directory.Exists(cfaPath))
                {
                    cciCommand += $" {folderPath}/{i}.cfa";
                }
            }
            cciCommand += $" {dirPath}/{fileName}.3ds --header {folderPath}/ncsdheader.bin";
            RunCommand(cciCommand);
            MessageBox.Show("3DS ROM打包成功完成", "成功了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnPack_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderBrowserDialog.SelectedPath;
                    PackExeFsRomFs(folderPath);
                    PackCxiCfa(folderPath);
                    PackCci(folderPath);
                }
            }
        }
    }
}