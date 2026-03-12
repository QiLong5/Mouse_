using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OfficeOpenXml;

/// <summary>
/// Excel读取工具类
/// 基于EPPlus库实现Excel文件的读取功能
/// </summary>
public class ExcelReader
{
    /// <summary>
    /// 读取Excel文件并按行输出数据
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    public void ReadExcelByRow(string filePath, int sheetIndex = 1)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count < sheetIndex)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}");
                    return;
                }

                // EPPlus的工作表索引是从1开始的
                var worksheet = package.Workbook.Worksheets[sheetIndex];
                Debug.Log($"正在读取工作表: {worksheet.Name}");
                Debug.Log($"总行数: {worksheet.Dimension.Rows}, 总列数: {worksheet.Dimension.Columns}");
                Debug.Log("----------------------------------------");

                // 按行读取并输出
                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    string rowData = $"第{row}行: ";
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        string value = cellValue != null ? cellValue.ToString() : "空";
                        rowData += $"[列{col}: {value}] ";
                    }
                    Debug.Log(rowData);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}");
        }
    }

    /// <summary>
    /// 读取Excel文件并按行返回数据
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    /// <returns>每行数据的列表，每行是一个字符串数组</returns>
    public List<List<string>> ReadExcelData(string filePath, int sheetIndex = 1)
    {
        List<List<string>> allData = new List<List<string>>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return allData;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count < sheetIndex)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}");
                    return allData;
                }

                // EPPlus的工作表索引是从1开始的
                var worksheet = package.Workbook.Worksheets[sheetIndex];

                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    List<string> rowData = new List<string>();
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        rowData.Add(cellValue != null ? cellValue.ToString() : "");
                    }
                    allData.Add(rowData);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}");
        }

        return allData;
    }

    /// <summary>
    /// 读取Excel文件指定工作表名称
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetName">工作表名称</param>
    public void ReadExcelBySheetName(string filePath, string sheetName)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    Debug.LogError($"未找到名为 '{sheetName}' 的工作表");
                    return;
                }

                Debug.Log($"正在读取工作表: {worksheet.Name}");
                Debug.Log($"总行数: {worksheet.Dimension.Rows}, 总列数: {worksheet.Dimension.Columns}");
                Debug.Log("----------------------------------------");

                for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                {
                    string rowData = $"第{row}行: ";
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        string value = cellValue != null ? cellValue.ToString() : "空";
                        rowData += $"[列{col}: {value}] ";
                    }
                    Debug.Log(rowData);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}");
        }
    }

    /// <summary>
    /// 获取Excel文件中所有工作表的名称
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <returns>工作表名称列表</returns>
    public List<string> GetAllSheetNames(string filePath)
    {
        List<string> sheetNames = new List<string>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return sheetNames;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    sheetNames.Add(worksheet.Name);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}");
        }

        return sheetNames;
    }

    /// <summary>
    /// 读取带表头的Excel文件（第1行：英文名，第2行：类型，第3行：中文说明，第4行起：数据）
    /// 只读取有数据的单元格，按中文说明输出
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    public void ReadExcelWithHeader(string filePath, int sheetIndex = 1)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                Debug.Log($"Excel文件工作表总数: {package.Workbook.Worksheets.Count}");

                if (package.Workbook.Worksheets.Count == 0)
                {
                    Debug.LogError("Excel文件中没有工作表！");
                    return;
                }

                if (sheetIndex < 1 || sheetIndex > package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}，有效范围: 1-{package.Workbook.Worksheets.Count}");
                    return;
                }

                // EPPlus的工作表索引是从1开始的，不是从0开始
                var worksheet = package.Workbook.Worksheets[sheetIndex];

                if (worksheet.Dimension.Rows < 4)
                {
                    Debug.LogError("Excel格式错误：至少需要4行（表头3行+数据1行）");
                    return;
                }

                // 读取表头信息
                List<string> englishNames = new List<string>();
                List<string> dataTypes = new List<string>();
                List<string> chineseNames = new List<string>();
                List<int> validColumns = new List<int>(); // 记录有效列索引

                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var englishValue = worksheet.Cells[1, col].Value;
                    var typeValue = worksheet.Cells[2, col].Value;
                    var chineseValue = worksheet.Cells[3, col].Value;

                    // 只添加有数据的列
                    if (englishValue != null && !string.IsNullOrWhiteSpace(englishValue.ToString()))
                    {
                        englishNames.Add(englishValue.ToString());
                        dataTypes.Add(typeValue != null ? typeValue.ToString() : "");
                        chineseNames.Add(chineseValue != null ? chineseValue.ToString() : "");
                        validColumns.Add(col);
                    }
                }

                Debug.Log($"=== 正在读取工作表: {worksheet.Name} ===");
                Debug.Log($"有效列数: {validColumns.Count}, 数据行数: {worksheet.Dimension.Rows - 3}");
                Debug.Log("表头信息:");
                for (int i = 0; i < englishNames.Count; i++)
                {
                    Debug.Log($"  [{englishNames[i]}] ({dataTypes[i]}) - {chineseNames[i]}");
                }
                Debug.Log("========================================");

                // 读取数据行（从第4行开始）
                for (int row = 4; row <= worksheet.Dimension.Rows; row++)
                {
                    List<string> rowValues = new List<string>();
                    bool hasData = false;

                    // 检查这一行是否有数据
                    for (int i = 0; i < validColumns.Count; i++)
                    {
                        var cellValue = worksheet.Cells[row, validColumns[i]].Value;
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            hasData = true;
                            rowValues.Add(cellValue.ToString());
                        }
                        else
                        {
                            rowValues.Add("");
                        }
                    }

                    // 只输出有数据的行
                    if (hasData)
                    {
                        string output = $"第{row - 3}条数据: ";
                        List<string> fields = new List<string>();

                        for (int i = 0; i < chineseNames.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(rowValues[i]))
                            {
                                fields.Add($"{chineseNames[i]}: {rowValues[i]}");
                            }
                        }

                        output += string.Join(", ", fields.ToArray());
                        Debug.Log(output);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 读取带表头的Excel文件并返回结构化数据
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    /// <returns>ExcelTableData对象，包含表头和数据</returns>
    public ExcelTableData ReadExcelWithHeaderData(string filePath, int sheetIndex = 1)
    {
        ExcelTableData tableData = new ExcelTableData();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return tableData;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                Debug.Log($"Excel文件工作表总数: {package.Workbook.Worksheets.Count}");

                if (package.Workbook.Worksheets.Count == 0)
                {
                    Debug.LogError("Excel文件中没有工作表！");
                    return tableData;
                }

                if (sheetIndex < 1 || sheetIndex > package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}，有效范围: 1-{package.Workbook.Worksheets.Count}");
                    return tableData;
                }

                // EPPlus的工作表索引是从1开始的，不是从0开始
                var worksheet = package.Workbook.Worksheets[sheetIndex];
                tableData.sheetName = worksheet.Name;

                if (worksheet.Dimension.Rows < 4)
                {
                    Debug.LogError("Excel格式错误：至少需要4行（表头3行+数据1行）");
                    return tableData;
                }

                // 读取表头信息
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var englishValue = worksheet.Cells[1, col].Value;

                    // 只添加有数据的列
                    if (englishValue != null && !string.IsNullOrWhiteSpace(englishValue.ToString()))
                    {
                        ExcelColumnInfo columnInfo = new ExcelColumnInfo
                        {
                            englishName = englishValue.ToString(),
                            dataType = worksheet.Cells[2, col].Value?.ToString() ?? "",
                            chineseName = worksheet.Cells[3, col].Value?.ToString() ?? "",
                            columnIndex = col
                        };
                        tableData.columns.Add(columnInfo);
                    }
                }

                // 读取数据行（从第4行开始）
                for (int row = 4; row <= worksheet.Dimension.Rows; row++)
                {
                    Dictionary<string, string> rowData = new Dictionary<string, string>();
                    bool hasData = false;

                    foreach (var columnInfo in tableData.columns)
                    {
                        var cellValue = worksheet.Cells[row, columnInfo.columnIndex].Value;
                        string value = cellValue != null ? cellValue.ToString() : "";

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            hasData = true;
                        }

                        rowData[columnInfo.englishName] = value;
                    }

                    // 只添加有数据的行
                    if (hasData)
                    {
                        tableData.rows.Add(rowData);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}\n{e.StackTrace}");
        }

        return tableData;
    }

    /// <summary>
    /// 检测工作表的表格类型（在第一行查找"参数表"或"结构列表"关键字）
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    /// <returns>表格类型：参数表、结构列表、未知</returns>
    public string DetectTableType(string filePath, int sheetIndex = 1)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return "未知";
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    Debug.LogError("Excel文件中没有工作表！");
                    return "未知";
                }

                if (sheetIndex < 1 || sheetIndex > package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}");
                    return "未知";
                }

                var worksheet = package.Workbook.Worksheets[sheetIndex];

                // 在第一行查找"参数表"或"结构列表"关键字
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[1, col].Value;
                    if (cellValue != null)
                    {
                        string tableType = cellValue.ToString().Trim();
                        if (tableType == "参数表" || tableType == "结构列表")
                        {
                            return tableType;
                        }
                    }
                }

                // 如果第一行没有找到已知类型标识，默认为参数表（向后兼容）
                return "参数表";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"检测表格类型时发生错误: {e.Message}");
            return "未知";
        }
    }

    /// <summary>
    /// 读取横向配置表（参数表格式）
    /// 格式：第1行找到"参数表"所在的列，从该列开始：第1列=中文注释，第2列=变量名，第3列=值，第4列=类型
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    /// <returns>横向配置数据</returns>
    public ExcelHorizontalConfigData ReadHorizontalConfig(string filePath, int sheetIndex = 1)
    {
        ExcelHorizontalConfigData configData = new ExcelHorizontalConfigData();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return configData;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    Debug.LogError("Excel文件中没有工作表！");
                    return configData;
                }

                if (sheetIndex < 1 || sheetIndex > package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}");
                    return configData;
                }

                var worksheet = package.Workbook.Worksheets[sheetIndex];
                configData.sheetName = worksheet.Name;

                // 在第一行查找"参数表"关键字所在的列
                int startCol = 1;
                bool foundKeyword = false;
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[1, col].Value;
                    if (cellValue != null && cellValue.ToString().Trim() == "参数表")
                    {
                        startCol = col;
                        foundKeyword = true;
                        Debug.Log($"[ExcelTool] 在第{col}列找到【参数表】关键字，从此列开始读取数据");
                        break;
                    }
                }

                int startRow = 3; // 默认从第3行开始读取数据
                if (foundKeyword)
                {
                    // 新格式：第1行有"参数表"关键字，第2行=表头，第3行开始=数据
                    startRow = 3;
                    Debug.Log($"[ExcelTool] 检测到参数表格式: {configData.sheetName}");
                }
                else
                {
                    // 旧格式兼容：没有"参数表"标识，第1行=表头，第2行开始=数据
                    startRow = 2;
                    startCol = 1;
                    Debug.Log($"[ExcelTool] 使用旧格式兼容模式: {configData.sheetName}");
                }

                // 从指定行开始读取数据（每行是一个字段）
                for (int row = startRow; row <= worksheet.Dimension.Rows; row++)
                {
                    // 读取4列：注释、变量名、值、类型（从startCol开始）
                    var commentValue = worksheet.Cells[row, startCol].Value;
                    var fieldNameValue = worksheet.Cells[row, startCol + 1].Value;
                    var valueValue = worksheet.Cells[row, startCol + 2].Value;
                    var typeValue = worksheet.Cells[row, startCol + 3].Value;

                    // 只处理有变量名的行
                    if (fieldNameValue != null && !string.IsNullOrWhiteSpace(fieldNameValue.ToString()))
                    {
                        ExcelFieldInfo fieldInfo = new ExcelFieldInfo
                        {
                            comment = commentValue != null ? commentValue.ToString() : "",
                            fieldName = fieldNameValue.ToString().Trim(),
                            value = valueValue != null ? valueValue.ToString() : "",
                            dataType = typeValue != null ? typeValue.ToString() : "string"
                        };

                        configData.fields.Add(fieldInfo);
                    }
                }

                Debug.Log($"[ExcelTool] 读取参数表: {configData.sheetName}, 共 {configData.fields.Count} 个字段");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取Excel文件时发生错误: {e.Message}\n{e.StackTrace}");
        }

        return configData;
    }

    /// <summary>
    /// 读取结构列表（List格式）
    /// 格式：第1行找到"结构列表"所在的列，从该列开始：第1行=参数类型，第2行=表头说明，第3行：第1列=列表名，第2列开始=参数名，第4行开始=数据
    /// </summary>
    /// <param name="filePath">Excel文件路径</param>
    /// <param name="sheetIndex">工作表索引（从1开始）</param>
    /// <returns>结构列表数据</returns>
    public ExcelStructListData ReadStructList(string filePath, int sheetIndex = 1)
    {
        ExcelStructListData structData = new ExcelStructListData();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Excel文件不存在: {filePath}");
            return structData;
        }

        try
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    Debug.LogError("Excel文件中没有工作表！");
                    return structData;
                }

                if (sheetIndex < 1 || sheetIndex > package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"工作表索引超出范围: {sheetIndex}");
                    return structData;
                }

                var worksheet = package.Workbook.Worksheets[sheetIndex];
                structData.sheetName = worksheet.Name;

                // 在第一行查找"结构列表"关键字所在的列
                int startCol = 1;
                bool foundKeyword = false;
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var cellValue = worksheet.Cells[1, col].Value;
                    if (cellValue != null && cellValue.ToString().Trim() == "结构列表")
                    {
                        startCol = col;
                        foundKeyword = true;
                        Debug.Log($"[ExcelTool] 在第{col}列找到【结构列表】关键字，从此列开始读取数据");
                        break;
                    }
                }

                if (!foundKeyword)
                {
                    Debug.LogWarning($"[ExcelTool] 未在第一行找到【结构列表】关键字，默认从第1列开始读取");
                    startCol = 1;
                }

                // 读取列表名（第3行第startCol列）
                var listNameCell = worksheet.Cells[3, startCol].Value;
                structData.listName = listNameCell != null ? listNameCell.ToString().Trim() : "dataList";

                // 读取参数信息（从startCol+1列开始）
                for (int col = startCol + 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var paramTypeCell = worksheet.Cells[1, col].Value; // 第1行：参数类型
                    var commentCell = worksheet.Cells[2, col].Value;    // 第2行：中文说明
                    var paramNameCell = worksheet.Cells[3, col].Value;  // 第3行：参数名

                    // 只处理有参数名的列
                    if (paramNameCell != null && !string.IsNullOrWhiteSpace(paramNameCell.ToString()))
                    {
                        ExcelStructParam param = new ExcelStructParam
                        {
                            paramName = paramNameCell.ToString().Trim(),
                            paramType = paramTypeCell != null ? paramTypeCell.ToString().Trim() : "string",
                            comment = commentCell != null ? commentCell.ToString().Trim() : "",
                            columnIndex = col
                        };

                        structData.parameters.Add(param);
                    }
                }

                // 读取数据行（从第4行开始）
                for (int row = 4; row <= worksheet.Dimension.Rows; row++)
                {
                    Dictionary<string, string> rowData = new Dictionary<string, string>();
                    bool hasData = false;

                    // 读取第startCol列的序号（可选）
                    var indexCell = worksheet.Cells[row, startCol].Value;
                    if (indexCell != null && !string.IsNullOrWhiteSpace(indexCell.ToString()))
                    {
                        rowData["_index"] = indexCell.ToString();
                        hasData = true;
                    }

                    // 读取每个参数的值
                    foreach (var param in structData.parameters)
                    {
                        var cellValue = worksheet.Cells[row, param.columnIndex].Value;
                        string value = cellValue != null ? cellValue.ToString() : "";

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            hasData = true;
                        }

                        rowData[param.paramName] = value;
                    }

                    // 只添加有数据的行
                    if (hasData)
                    {
                        structData.rows.Add(rowData);
                    }
                }

                Debug.Log($"[ExcelTool] 读取结构列表: {structData.sheetName}, 列表名={structData.listName}, 参数={structData.parameters.Count}, 数据行={structData.rows.Count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取结构列表时发生错误: {e.Message}\n{e.StackTrace}");
        }

        return structData;
    }
}

/// <summary>
/// Excel列信息
/// </summary>
[System.Serializable]
public class ExcelColumnInfo
{
    public string englishName;   // 英文字段名
    public string dataType;      // 数据类型
    public string chineseName;   // 中文说明
    public int columnIndex;      // 列索引
}

/// <summary>
/// Excel表格数据
/// </summary>
[System.Serializable]
public class ExcelTableData
{
    public string sheetName;                              // 工作表名称
    public List<ExcelColumnInfo> columns = new List<ExcelColumnInfo>();  // 列信息
    public List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();  // 数据行
}

/// <summary>
/// Excel字段信息（横向配置用）
/// </summary>
[System.Serializable]
public class ExcelFieldInfo
{
    public string comment;    // 中文注释
    public string fieldName;  // 变量名
    public string value;      // 值
    public string dataType;   // 数据类型
}

/// <summary>
/// Excel横向配置数据（参数表）
/// </summary>
[System.Serializable]
public class ExcelHorizontalConfigData
{
    public string sheetName;  // 工作表名称
    public List<ExcelFieldInfo> fields = new List<ExcelFieldInfo>();  // 字段列表
}

/// <summary>
/// Excel结构参数信息（结构列表用）
/// </summary>
[System.Serializable]
public class ExcelStructParam
{
    public string paramName;   // 参数名
    public string paramType;   // 参数类型
    public string comment;     // 中文说明
    public int columnIndex;    // 列索引
}

/// <summary>
/// Excel结构列表数据（List格式）
/// </summary>
[System.Serializable]
public class ExcelStructListData
{
    public string sheetName;        // 工作表名称
    public string listName;         // 列表名称（A3单元格）
    public List<ExcelStructParam> parameters = new List<ExcelStructParam>();  // 参数列表
    public List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();  // 数据行
}
