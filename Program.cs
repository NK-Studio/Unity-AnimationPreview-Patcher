using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NKStudio
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("어떤 패치를 할까요?");
            Console.Write("0: 코드 패치, 1: 코드 또는 폴더 열기, 2: 종료 -> ");
            string index = Console.ReadLine();

            int id = int.Parse(index);

            switch (id)
            {
                case 0:
                    Console.Clear();
                    Console.WriteLine("패치를 시작합니다.");
                    Patch();
                    break;
                case 1:
                    Console.Clear();
                    Console.WriteLine("코드 또는 폴더를 엽니다.");
                    OpenFolder();
                    break;
                case 2:
                    return;
            }
        }

        private static void OpenFolder()
        {
            // 현재 켜져있는 모든 프로세스를 가져온다.
            // 백그라운드에서 동작하는 프로그램은 제외한다.
            List<Process> processes =
                Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();

            // process.MainWindowTitle에서 Unity라는 이름을 가진 녀석 외에 제거한다.
            processes = processes.Where(p => p.MainWindowTitle.Contains("Unity")).ToList();

            // Unity Hub를 제외한다.
            processes = processes.Where(p => !p.MainWindowTitle.Contains("Unity Hub")).ToList();

            if (processes.Count == 0)
            {
                Console.WriteLine("Unity가 실행되어있지 않습니다.");
                return;
            }

            int patchNumber = 0;

            if (processes.Count > 1)
            {
                Console.WriteLine("유니티가 여러개 실행되어 있어요. 어떤 것을 오픈할까요?");
                // 이름을 보여준다.
                for (int i = 0; i < processes.Count; i++)
                    Debug.Log($"{i} : " + processes[i].MainWindowTitle);

                Console.Write("오픈할 유니티 번호를 입력하세요 -> ");
                patchNumber = int.Parse(Console.ReadLine() ?? "0");
                Console.Clear();
            }

            Console.WriteLine(processes[patchNumber].MainWindowTitle + "를 오픈합니다.");

            // 로그로 이름을 보여준다

            var path = processes[patchNumber].MainModule.FileName;
            // 확장자는 제거하고 경로만 얻는다
            path = Path.GetDirectoryName(path);

            var targetCode = path +
                             @"\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Runtime";

            
            Console.WriteLine("코드를 오픈할까요? 폴더를 오픈할까요?");
            Console.Write("0: 코드, 1: 폴더 -> ");
            int index = int.Parse(Console.ReadLine() ?? "1");

            switch (index)
            {
                case 0:
                    targetCode += @"\UniversalRenderPipelineCore.cs";
                    Process.Start(targetCode);
                    break;
                
                case 1:
                    Process.Start(targetCode);
                    break;
                
            }
            Console.WriteLine();
            Console.WriteLine("종료하려면 아무 키나 누르세요.");
            Console.ReadKey();
        }

        private static void Patch()
        {
            // 현재 켜져있는 모든 프로세스를 가져온다.
            // 백그라운드에서 동작하는 프로그램은 제외한다.
            List<Process> processes =
                Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();

            // process.MainWindowTitle에서 Unity라는 이름을 가진 녀석 외에 제거한다.
            processes = processes.Where(p => p.MainWindowTitle.Contains("Unity")).ToList();

            // Unity Hub를 제외한다.
            processes = processes.Where(p => !p.MainWindowTitle.Contains("Unity Hub")).ToList();

            if (processes.Count == 0)
            {
                Console.WriteLine("Unity가 실행되어있지 않습니다.");
                return;
            }

            int patchNumber = 0;

            if (processes.Count > 1)
            {
                Console.WriteLine("유니티가 여러개 실행되어 있어요. 어떤 것을 패치할까요?");
                // 이름을 보여준다.
                for (int i = 0; i < processes.Count; i++)
                    Debug.Log($"{i} : " + processes[i].MainWindowTitle);

                Console.Write("패치할 유니티 번호를 입력하세요 -> ");
                patchNumber = int.Parse(Console.ReadLine() ?? "1");
                Console.Clear();
            }

            Console.WriteLine(processes[patchNumber].MainWindowTitle + "를 패치합니다.");

            // 로그로 이름을 보여준다

            var path = processes[patchNumber].MainModule.FileName;
            // 확장자는 제거하고 경로만 얻는다
            path = Path.GetDirectoryName(path);

            var targetCode = path +
                             @"\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Runtime\UniversalRenderPipelineCore.cs";

            //파일이 있는지 체크
            if (File.Exists(targetCode))
            {
                // 파일이 있으면 읽어온다
                string[] text = File.ReadAllText(targetCode).Split('\n');

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Contains(
                            "if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)"))
                    {
                        Console.WriteLine("이미 패치가 되어있습니다.");
                        break;
                    }

                    if (text[i].Contains("if (cameraType == CameraType.SceneView)"))
                    {
                        text[i] = text[i].Replace("if (cameraType == CameraType.SceneView)",
                            "if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)");

                        // text을 다시 합친다
                        string newText = string.Join("\n", text);
                        File.WriteAllText(targetCode, newText);
                        Console.WriteLine("패치 완료되었습니다.");
                        Console.WriteLine();
                        Console.WriteLine("유니티 프로젝트를 강제로 종료합니다.");
                        Console.WriteLine();
                        processes[patchNumber].Kill();
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Error");
            }


            Console.WriteLine("종료하려면 아무 키나 누르세요.");
            Console.ReadKey();
        }
    }
}