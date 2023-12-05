using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NKStudio
{
    public class APCore : Editor
    {
        private enum Style
        {
            Install,
            UnInstall
        }

        private const string InstallScript = @"
        $path = '$CodePath$'
        $content = Get-Content $path
        $flag = $false
        $newContent = $content | ForEach-Object {
            if ($_ -match 'if \(cameraType == CameraType.SceneView\)' -and -not $flag) {
                $flag = $true
                $_ -replace 'if \(cameraType == CameraType.SceneView\)', 'if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)'
            } else {
                $_
            }
        }
        Set-Content -Path $path -Value $newContent
        ";

        private const string UnInstallScript = @"
        $path = '$CodePath$'
        $content = Get-Content $path
        $newContent = $content -replace 'if \(cameraType == CameraType.SceneView \|\| cameraType == CameraType.Preview\)', 'if (cameraType == CameraType.SceneView)'
        Set-Content -Path $path -Value $newContent
        ";

        /// <summary>
        /// UniversalRenderPipelineCore에 코드를 추가합니다.
        /// </summary>
        public static void Install()
        {
            InstallSystemCore(Style.Install);
            InstallPackageCore(Style.Install);
        }


        /// <summary>
        /// UniversalRenderPipelineCore에 추가한 코드를 제거합니다.
        /// </summary>
        public static void UnInstall()
        {
            InstallSystemCore(Style.UnInstall);
            InstallPackageCore(Style.UnInstall);
        }

        /// <summary>
        /// 유니티가 설치되어있는 폴더에 있는 Core 코드를 수정합니다.
        /// </summary>
        /// <param name="style"></param>
        private static void InstallSystemCore(Style style)
        {
            // // 현재 실행 중인 프로세스를 가져옴
            Process currentProcess = Process.GetCurrentProcess();

            // 프로세스의 실행 경로를 가져옴
            if (currentProcess.MainModule != null)
            {
                string exePath = currentProcess.MainModule.FileName;

                // 실행 파일의 폴더 경로를 추출
                string exeFolder = Path.GetDirectoryName(exePath);

                // 타겟 파일 경로
                string target = exeFolder +
                                @"\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.universal\Runtime\UniversalRenderPipelineCore.cs";

                string triggerScript;
                if (style == Style.Install)
                    triggerScript = InstallScript.Replace("$CodePath$", target);
                else
                    triggerScript = UnInstallScript.Replace("$CodePath$", target);

                var preCode = File.ReadAllText(target);
                if (style == Style.Install)
                {
                
                    var hasHook = preCode.Contains("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)");
                
                    if (hasHook)
                        return;
                }
                else
                {
                    var hasNotHook = !preCode.Contains("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)");
                    if (hasNotHook)
                        return;
                }
            
                try
                {
                    // PowerShell 프로세스 시작
                    ProcessStartInfo psi = new()
                    {
                        FileName = "powershell.exe",
                        Verb = "runas", // 관리자 권한으로 실행
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new() { StartInfo = psi };
                    process.Start();

                    // 스크립트 실행
                    process.StandardInput.WriteLine(triggerScript);
                    process.StandardInput.WriteLine("exit");

                    process.WaitForExit();
                    process.Close();
                }
                catch (Exception ex)
                {
                    Debug.Log("오류 발생: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 유니티 패키지에 있는 Core 코드를 수정합니다.
        /// </summary>
        /// <param name="style"></param>
        private static void InstallPackageCore(Style style)
        {
            string target = Application.dataPath.Replace("Assets", "") +
                            "Packages/com.unity.render-pipelines.universal/Runtime/UniversalRenderPipelineCore.cs";
            if (style == Style.Install)
            {
                string[] textLines = File.ReadAllLines(target);

                foreach (string textLine in textLines)
                {
                    if (textLine.Contains("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)"))
                        return;
                }
                
                for (int i = 0; i < textLines.Length; i++)
                {
                    if (textLines[i].Contains("if (cameraType == CameraType.SceneView)"))
                    {
                        textLines[i] = textLines[i].Replace("if (cameraType == CameraType.SceneView)",
                            "if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)");
                        break;
                    }
                }

                File.WriteAllLines(target, textLines);
                Debug.Log("패치 완료");
            }
            else
            {
                string text = File.ReadAllText(target);

                bool hasNotCode = !text.Contains("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)");
                
                if (hasNotCode)
                    return;

                text = text.Replace("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)",
                    "if (cameraType == CameraType.SceneView)");
                File.WriteAllText(target, text);
                Debug.Log("제거 완료");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 패치가 되어있으면 True를 반환한다.
        /// </summary>
        /// <returns></returns>
        public static bool IsInstalled()
        {
            string target = Application.dataPath.Replace("Assets", "") +
                            "Packages/com.unity.render-pipelines.universal/Runtime/UniversalRenderPipelineCore.cs";
            string[] textLines = File.ReadAllLines(target);

            for (int i = 0; i < textLines.Length; i++)
                if (textLines[i]
                    .Contains("if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)"))
                    return true;

            return false;
        }
    }
}
