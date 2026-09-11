using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// CSV 파일을 JSON 형식으로 변환하고, 기존 JSON 파일과 병합할 수 있도록 함
/// <summary>

/*
 사용방법
    1. Unity 에디터에서 GameObject를 선택하고, ConvertCsvToJson 스크립트를 추가합니다.
    2. Inspector에서 CSV 파일 경로와 출력 JSON 파일명을 설정합니다.
    3. (선택 사항) 기존 JSON 파일과 병합하려면, 병합할 JSON 파일 경로를 설정합니다.
    4. Inspector에서 "Convert CSV to JSON File" 버튼을 클릭하여 변환을 실행합니다.
    5. 변환이 완료되면, 지정한 출력 경로에 JSON 파일이 생성됩니다.
 */
public class ConvertCsvToJson : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Cory Path로 복사되는 전체 경로 붙여넣기")]
    [SerializeField] private string csvFilePath = "Assets/Test/SetData/ItemData - 시트1.csv";

    [Tooltip("생성할 파일명만 입력하면 CSV와 같은 폴더에 자동으로 생성됨.")]
    [SerializeField] private string jsonOutputFileName = "ItemData_Converted";

    [Header("Merge Settings (Optional)")]
    [Tooltip("병합할 기존 JSON/txt 파일의 경로 (비어있으면 병합 없이 신규 생성)")]
    [SerializeField] private string mergeJsonFilePath = "";

    [ContextMenu("Convert CSV to JSON File")]
    public void ConvertCsvToJsonFile()
    {
        string fullCsvPath = ResolvePath(csvFilePath, ".csv"); //fullCsvPath에 csvFilePath를 ResolvePath 메서드에 전달하여 절대 경로로 변환하고 저장. 기본 확장자는 ".csv"로 지정

        if (!File.Exists(fullCsvPath))
        {
            Debug.LogError($"[DataTableManager] CSV 파일을 찾을 수 없습니다: {fullCsvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(fullCsvPath);
        if (lines.Length < 3)
        {
            Debug.LogWarning($"[DataTableManager] CSV 파일의 데이터가 부족합니다. (최소 3줄 필요, 현재: {lines.Length}줄)");
            return;
        }

        string[] headers = ParseCsvLine(lines[1]); //ParseCsvLine 메서드를 호출하여 CSV 라인을 분리하고 두번째 줄(인덱스 1)부터 headers 배열에 저장
        var dataList = new List<Dictionary<string, string>>(); //데이터를 담을 List<Dictionary<string, string>> 타입의 dataList를 생성. 각 행은 Dictionary로 저장되며, Key는 헤더명, Value는 해당 행의 값

        for (int i = 2; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue; //빈 줄은 건너뜀

            string[] values = ParseCsvLine(lines[i]); //ParseCsvLine 메서드를 호출하여 CSV 라인을 분리하고 values 배열에 저장
            Dictionary<string, string> row = new Dictionary<string, string>(); //각 행을 담을 Dictionary<string, string> 타입의 row를 생성. Key는 헤더명, Value는 해당 행의 값

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                string headerName = headers[j]; //headers 배열에서 j번째 원소를 가져와 headerName에 저장
                if (!string.IsNullOrEmpty(headerName)) //헤더명이 비어있지 않으면
                {
                    row[headerName] = values[j]; //row 딕셔너리에 Key로 headerName, Value로 values[j]를 추가
                }
            }

            dataList.Add(row); //row 딕셔너리를 dataList에 추가
        }

        //기존 JSON 파일과 병합 처리
        var finalItems = new List<Dictionary<string, string>>(); //최종적으로 저장할 데이터 리스트를 담을 List<Dictionary<string, string>> 타입의 finalItems 선언 및 인스턴스화. 
        string fullMergePath = ResolvePath(mergeJsonFilePath, ".txt"); //mergeJsonFilePath를 ResolvePath 메서드에 전달하여 fullMergePath에 저장. 기본 확장자는 ".txt"로 지정 

        if (!string.IsNullOrEmpty(fullMergePath) && File.Exists(fullMergePath))
        {
            try
            {
                string existingJson = File.ReadAllText(fullMergePath); //기존 JSON 파일 내용을 읽어 existingJson에 저장

                // { "items": [...] } 구조 파싱 시도
                var existingRoot = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(existingJson); //existingJson을 Dictionary<string, List<Dictionary<string, string>>> 타입으로 역직렬화하여 existingRoot에 저장.
                if (existingRoot != null && existingRoot.ContainsKey("items") && existingRoot["items"] != null) //existingRoot가 null이 아니고, "items" 키가 존재하며(ContainsKey), 해당 값이 null이 아니면 => 사용자가 병합할 파일을 넣었을 경우
                {
                    finalItems = existingRoot["items"]; //finalItems에 existingRoot 속 items라는 키에 연결된 value = List<Dictionary<string, string>> 값을 할당
                }
                else
                {
                    // [...] 단순 배열 구조 파싱 시도
                    var existingList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(existingJson); //existingJson을 List<Dictionary<string, string>> 타입으로 역직렬화하여 existingList에 저장. (기존 JSON 파일이 단순 배열 구조일 경우)
                    if (existingList != null)
                    {
                        finalItems = existingList; //finalItems에 existingList 값을 할당
                    }
                }

                Debug.Log($"<color=yellow>[병합 진행]</color> 기존 파일에서 {finalItems.Count}개의 데이터를 성공적으로 읽어왔습니다.");
            }
            catch (Exception ex) //예외 발생 시
            {
                Debug.LogWarning($"[DataTableManager] 기존 JSON 파일 파싱 실패. 병합 없이 신규 생성합니다. (원인: {ex.Message})");
            }
        }

        // 기존 데이터 리스트 뒤에 새로 파싱한 CSV 데이터를 추가(병합)
        finalItems.AddRange(dataList); //finalItems에 dataList의 모든 요소를 추가

        var rootData = new Dictionary<string, object>  //var 키워드를 사용해 rootData 선언, 딕셔너리로 인스턴스화. Key는 문자열("items"), Value는 여러 타입이 들어올 수 있도록 object로 지정
        {
            { "items", finalItems } //items라는 Key에 finalItems를 Value로 넣음. 
        };

        string jsonText = JsonConvert.SerializeObject(rootData, Formatting.Indented); //rootData를 indented(들여쓰기)가 적용된 JSON 문자열로 변환
        string fullOutputPath = ResolveOutputPath(fullCsvPath, jsonOutputFileName); //저장 경로 결정 (파일명만 적은 경우 CSV와 동일한 폴더에 생성) 

        File.WriteAllText(fullOutputPath, jsonText); //jsonText를 fullOutputPath 경로에 파일로 저장

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log($"<color=green>[변환 완료]</color> 총 {dataList.Count}개 행을 '{Path.GetFileName(fullOutputPath)}'로 성공적으로 변환했습니다.");
    }

    // CSV 라인 분리 함수
    private string[] ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        MatchCollection matches = Regex.Matches(line, @"(?<=^|,)(?:""(?<val>[^""]*)""|(?<val>[^,]*))");

        foreach (Match match in matches)
        {
            result.Add(match.Groups["val"].Value.Trim());
        }

        return result.ToArray();
    }

    // 경로 정제 헬퍼 함수 (Assets/... 또는 C:/... 모두 대응)
    private string ResolvePath(string inputPath, string defaultExtension) //inputPath: 사용자가 입력한 경로, defaultExtension: 기본 확장자 (예: ".csv") 두개를 매개변수로 하는 메서드 ResolvePath
    {
        if (string.IsNullOrWhiteSpace(inputPath)) return ""; //입력 경로가 null이거나 공백이면 빈 문자열 반환
         
        inputPath = inputPath.Replace('\\', '/').Trim(); //경로 구분자를 모두 '/'로 통일하고, 앞뒤 공백 제거

        string fullPath = inputPath; //fullPath에 inputPath를 그대로 저장(확장자 유무 검사용)
        if (!Path.IsPathRooted(fullPath))
        {
            if (fullPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                fullPath = Path.Combine(projectRoot, fullPath);
            }
            else
            {
                fullPath = Path.Combine(Application.dataPath, fullPath);
            }
        }

        if (File.Exists(fullPath)) return fullPath; //fullPath에 해당하는 파일이 존재하면(Exists) fullPath를 반환

        // 확장자가 생략된 경우
        if (!Path.HasExtension(fullPath) && File.Exists(fullPath + defaultExtension)) //fullPath에 확장자가 없고(HasExtension), fullPath + defaultExtension 경로에 파일이 존재하면
        {
            return fullPath + defaultExtension;
        }

        // .csv 와 .txt 확장자 교차 탐색
        if (fullPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) //fullPath가 ".csv"로 끝나면(EndWith), StringComparison.OrdinalIgnoreCase은 대소문자 구분 없이 비교하도록 함.
        {
            string txtPath = Path.ChangeExtension(fullPath, ".txt");
            if (File.Exists(txtPath)) return txtPath;
        }
        else if (fullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            string csvPath = Path.ChangeExtension(fullPath, ".csv");
            if (File.Exists(csvPath)) return csvPath;
        }

        // 파일이 없을 경우 예외 메시지용 경로 반환
        return Path.HasExtension(fullPath) ? fullPath : fullPath + defaultExtension;
    }

    // 출력 경로 정제 헬퍼 함수
    private string ResolveOutputPath(string csvFullPath, string outputInput) //csvFullPath: CSV 파일의 전체 경로, outputInput: 사용자가 입력한 출력 파일명 두개를 매개변수로 하는 메서드 ResolveOutputPath
    {
        outputInput = outputInput.Replace('\\', '/').Trim(); //경로 구분자를 모두 '/'로 통일하고, 앞뒤 공백 제거해 outputInput에 저장
        if (!outputInput.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) //outputInput이 ".txt"로 끝나지 않으면
        {
            outputInput += ".txt"; //".txt"를 붙임
        }

        // 경로 구분자(/)가 들어간 경우 ResolvePath 적용
        if (outputInput.Contains("/")) //outputInput에 경로 구분자('/')가 포함되어 있으면
        {
            return ResolvePath(outputInput, ".txt"); //ResolvePath 메서드를 호출하여 outputInput을 절대 경로로 변환하고 반환
        }

        // 파일명만 적은 경우 CSV가 존재하는 디렉토리에 동일하게 저장
        string csvDir = Path.GetDirectoryName(csvFullPath); //csvFullPath의 디렉토리 경로를 구해 csvDir에 저장
        return Path.Combine(csvDir, outputInput); //csvDir과 outputInput을 결합하여 절대 경로로 반환
    }
}