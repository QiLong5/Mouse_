using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ExcelTool
{
    /// <summary>
    /// Excel转ScriptableObject工具
    /// 根据Excel表格生成ScriptableObject类和资源
    /// </summary>
    public class ExcelToScriptableObject
    {
        /// <summary>
        /// 根据Excel表生成ScriptableObject类代码
        /// </summary>
        public static string GenerateScriptableObjectClass(ExcelTableData tableData, string className)
        {
            StringBuilder sb = new StringBuilder();

            // 添加命名空间和引用
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace ExcelTool");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {tableData.sheetName} 配置数据");
            sb.AppendLine("    /// 自动生成，请勿手动修改");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"ExcelTool/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");

            // 生成字段
            foreach (var column in tableData.columns)
            {
                string fieldName = column.englishName;
                string dataType = ConvertToUnityType(column.dataType);
                string comment = column.chineseName;

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {comment}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {dataType} {fieldName};");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成包含所有数据的列表ScriptableObject类
        /// </summary>
        public static string GenerateListScriptableObjectClass(ExcelTableData tableData, string className, string itemClassName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace ExcelTool");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {tableData.sheetName} 配置列表");
            sb.AppendLine("    /// 自动生成，请勿手动修改");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"ExcelTool/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 所有{tableData.sheetName}数据");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public List<{itemClassName}> items = new List<{itemClassName}>();");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 数据项");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [System.Serializable]");
            sb.AppendLine($"        public class {itemClassName}");
            sb.AppendLine("        {");

            // 生成字段
            foreach (var column in tableData.columns)
            {
                string fieldName = column.englishName;
                string dataType = ConvertToUnityType(column.dataType);
                string comment = column.chineseName;

                sb.AppendLine($"            public {dataType} {fieldName}; // {comment}");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 根据横向配置表生成ScriptableObject类代码
        /// </summary>
        public static string GenerateHorizontalConfigClass(ExcelHorizontalConfigData configData, string className)
        {
            StringBuilder sb = new StringBuilder();

            // 添加命名空间和引用
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace ExcelTool");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {configData.sheetName} 配置数据");
            sb.AppendLine("    /// 自动生成，请勿手动修改");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"ExcelTool/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");

            // 生成字段（每个Excel行对应一个字段）
            foreach (var field in configData.fields)
            {
                string fieldName = field.fieldName;
                string dataType = ConvertToUnityType(field.dataType);
                string comment = field.comment;
                string defaultValue = field.value;

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {comment}");
                sb.AppendLine("        /// </summary>");

                // 生成字段，带默认值（不带行内注释）
                string valueString = GetFormattedValue(defaultValue, dataType);
                sb.AppendLine($"        public {dataType} {fieldName} = {valueString};");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成多表合并的ScriptableObject类代码
        /// </summary>
        public static string GenerateMultiSheetConfigClass(List<ExcelHorizontalConfigData> sheetsData, string className)
        {
            StringBuilder sb = new StringBuilder();

            // 添加命名空间和引用
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace ExcelTool");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {className} 多表配置数据");
            sb.AppendLine("    /// 自动生成，请勿手动修改");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"ExcelTool/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");

            // 添加元数据字段（在Inspector中折叠显示）
            sb.AppendLine("        [Header(\"=== 工具元数据（请勿手动修改） ===\")]");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 元数据：Excel文件路径");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [Tooltip(\"此文件由工具自动管理，请勿手动修改\")]");
            sb.AppendLine("        public string _excelFilePath = \"\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 元数据：使用的工作表索引（逗号分隔）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [Tooltip(\"此文件由工具自动管理，请勿手动修改\")]");
            sb.AppendLine("        public string _sheetIndices = \"\";");
            sb.AppendLine();
            sb.AppendLine("        [Space(20)]");

            // 为每个工作表生成一个字段和嵌套类
            foreach (var sheetData in sheetsData)
            {
                string fieldName = ConvertToCamelCase(SanitizeFieldName(sheetData.sheetName));
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {sheetData.sheetName}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public {nestedClassName} {fieldName} = new {nestedClassName}();");
                sb.AppendLine();
            }

            // 生成嵌套类
            foreach (var sheetData in sheetsData)
            {
                string nestedClassName = SanitizeClassName(sheetData.sheetName) + "Data";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {sheetData.sheetName} 数据");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        [System.Serializable]");
                sb.AppendLine($"        public class {nestedClassName}");
                sb.AppendLine("        {");

                // 生成字段
                foreach (var field in sheetData.fields)
                {
                    string fieldName = field.fieldName;
                    string dataType = ConvertToUnityType(field.dataType);
                    string comment = field.comment;
                    string defaultValue = field.value;

                    sb.AppendLine("            /// <summary>");
                    sb.AppendLine($"            /// {comment}");
                    sb.AppendLine("            /// </summary>");

                    string valueString = GetFormattedValue(defaultValue, dataType);
                    sb.AppendLine($"            public {dataType} {fieldName} = {valueString};");
                    sb.AppendLine();
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转换为驼峰命名（首字母小写）
        /// </summary>
        private static string ConvertToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "data";

            if (text.Length == 1)
                return text.ToLower();

            return char.ToLower(text[0]) + text.Substring(1);
        }

        /// <summary>
        /// 清理字段名
        /// </summary>
        private static string SanitizeFieldName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "data";

            // 移除非字母数字字符
            string result = "";
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result += c;
            }

            // 确保以字母开头
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "data" : result;
        }

        /// <summary>
        /// 清理类名
        /// </summary>
        private static string SanitizeClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Config";

            // 移除非字母数字字符
            string result = "";
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    result += c;
            }

            // 确保以字母开头
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "Config" : result;
        }

        /// <summary>
        /// 获取格式化的值（用于代码生成）
        /// </summary>
        private static string GetFormattedValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValueString(type);

            // 检查是否是List类型
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                // 提取泛型参数 List<int> -> int
                string innerType = type.Substring(5, type.Length - 6).Trim();
                return ParseListToString(value, innerType);
            }

            string lowerType = type.ToLower();

            switch (lowerType)
            {
                case "int":
                    return value;
                case "float":
                    // 确保float有f后缀
                    return value.Contains(".") ? value + "f" : value + ".0f";
                case "double":
                    return value.Contains(".") ? value : value + ".0";
                case "bool":
                    return value.ToLower();
                case "string":
                    return $"\"{value}\"";
                case "vector2":
                    return ParseVector2ToString(value);
                case "vector3":
                    return ParseVector3ToString(value);
                case "color":
                    return ParseColorToString(value);
                default:
                    return $"\"{value}\"";
            }
        }

        /// <summary>
        /// 解析List字符串为代码格式
        /// 支持格式：(1,2,3) 或 1,2,3
        /// </summary>
        private static string ParseListToString(string value, string innerType)
        {
            if (string.IsNullOrEmpty(value))
                return $"new List<{ConvertToUnityType(innerType)}>()";

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();

            if (string.IsNullOrWhiteSpace(value))
                return $"new List<{ConvertToUnityType(innerType)}>()";

            string[] parts = value.Split(',');
            List<string> formattedValues = new List<string>();

            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    // 根据内部类型格式化值
                    string formattedValue = GetFormattedValue(trimmedPart, innerType);
                    formattedValues.Add(formattedValue);
                }
            }

            string convertedInnerType = ConvertToUnityType(innerType);
            string valuesString = string.Join(", ", formattedValues.ToArray());
            return $"new List<{convertedInnerType}> {{ {valuesString} }}";
        }

        /// <summary>
        /// 解析Vector2字符串为代码格式
        /// 支持格式：(1,3) 或 1,3
        /// </summary>
        private static string ParseVector2ToString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Vector2.zero";

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 2)
            {
                string x = parts[0].Trim();
                string y = parts[1].Trim();

                // 确保数字有f后缀
                if (!x.EndsWith("f") && x.Contains(".")) x += "f";
                else if (!x.Contains(".")) x += ".0f";

                if (!y.EndsWith("f") && y.Contains(".")) y += "f";
                else if (!y.Contains(".")) y += ".0f";

                return $"new Vector2({x}, {y})";
            }

            Debug.LogWarning($"Vector2格式错误: {value}，使用默认值");
            return "Vector2.zero";
        }

        /// <summary>
        /// 解析Vector3字符串为代码格式
        /// 支持格式：(1,2,3) 或 1,2,3
        /// </summary>
        private static string ParseVector3ToString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Vector3.zero";

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 3)
            {
                string x = parts[0].Trim();
                string y = parts[1].Trim();
                string z = parts[2].Trim();

                // 确保数字有f后缀
                if (!x.EndsWith("f") && x.Contains(".")) x += "f";
                else if (!x.Contains(".")) x += ".0f";

                if (!y.EndsWith("f") && y.Contains(".")) y += "f";
                else if (!y.Contains(".")) y += ".0f";

                if (!z.EndsWith("f") && z.Contains(".")) z += "f";
                else if (!z.Contains(".")) z += ".0f";

                return $"new Vector3({x}, {y}, {z})";
            }

            Debug.LogWarning($"Vector3格式错误: {value}，使用默认值");
            return "Vector3.zero";
        }

        /// <summary>
        /// 解析Color字符串为代码格式
        /// 支持格式：(1,0,0,1) 或 1,0,0,1 (RGBA)
        /// </summary>
        private static string ParseColorToString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Color.white";

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 4)
            {
                string r = parts[0].Trim();
                string g = parts[1].Trim();
                string b = parts[2].Trim();
                string a = parts[3].Trim();

                // 确保数字有f后缀
                if (!r.EndsWith("f") && r.Contains(".")) r += "f";
                else if (!r.Contains(".")) r += ".0f";

                if (!g.EndsWith("f") && g.Contains(".")) g += "f";
                else if (!g.Contains(".")) g += ".0f";

                if (!b.EndsWith("f") && b.Contains(".")) b += "f";
                else if (!b.Contains(".")) b += ".0f";

                if (!a.EndsWith("f") && a.Contains(".")) a += "f";
                else if (!a.Contains(".")) a += ".0f";

                return $"new Color({r}, {g}, {b}, {a})";
            }
            else if (parts.Length == 3)
            {
                // RGB格式，默认alpha=1
                string r = parts[0].Trim();
                string g = parts[1].Trim();
                string b = parts[2].Trim();

                if (!r.EndsWith("f") && r.Contains(".")) r += "f";
                else if (!r.Contains(".")) r += ".0f";

                if (!g.EndsWith("f") && g.Contains(".")) g += "f";
                else if (!g.Contains(".")) g += ".0f";

                if (!b.EndsWith("f") && b.Contains(".")) b += "f";
                else if (!b.Contains(".")) b += ".0f";

                return $"new Color({r}, {g}, {b}, 1.0f)";
            }

            Debug.LogWarning($"Color格式错误: {value}，使用默认值");
            return "Color.white";
        }

        /// <summary>
        /// 将Excel类型转换为Unity C#类型
        /// 支持List<T>格式，如：List<int>, List<float>, List<string>
        /// </summary>
        private static string ConvertToUnityType(string excelType)
        {
            if (string.IsNullOrEmpty(excelType))
                return "string";

            string type = excelType.Trim();

            // 检查是否是List类型
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                // 提取泛型参数 List<int> -> int
                string innerType = type.Substring(5, type.Length - 6).Trim();
                string convertedInnerType = ConvertToUnityType(innerType);
                return $"List<{convertedInnerType}>";
            }

            string lowerType = type.ToLower();

            switch (lowerType)
            {
                case "int":
                case "int32":
                    return "int";
                case "float":
                case "single":
                    return "float";
                case "double":
                    return "double";
                case "bool":
                case "boolean":
                    return "bool";
                case "string":
                case "text":
                    return "string";
                case "long":
                case "int64":
                    return "long";
                case "vector2":
                    return "Vector2";
                case "vector3":
                    return "Vector3";
                case "color":
                    return "Color";
                default:
                    return "string";
            }
        }

        /// <summary>
        /// 将字符串值转换为指定类型的值
        /// </summary>
        public static object ConvertValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(type);

            // 检查是否是List类型
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                // 提取泛型参数 List<int> -> int
                string innerType = type.Substring(5, type.Length - 6).Trim();
                return ParseList(value, innerType);
            }

            try
            {
                switch (type.ToLower())
                {
                    case "int":
                    case "int32":
                        return int.Parse(value);
                    case "float":
                    case "single":
                        return float.Parse(value);
                    case "double":
                        return double.Parse(value);
                    case "bool":
                    case "boolean":
                        return bool.Parse(value);
                    case "long":
                    case "int64":
                        return long.Parse(value);
                    case "vector2":
                        return ParseVector2(value);
                    case "vector3":
                        return ParseVector3(value);
                    case "color":
                        return ParseColor(value);
                    default:
                        return value;
                }
            }
            catch
            {
                Debug.LogWarning($"无法将值 '{value}' 转换为类型 '{type}'，使用默认值");
                return GetDefaultValue(type);
            }
        }

        /// <summary>
        /// 解析List字符串为实际的List对象
        /// 支持格式：(1,2,3) 或 1,2,3
        /// </summary>
        private static object ParseList(string value, string innerType)
        {
            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();

            string[] parts = value.Split(',');

            // 根据内部类型创建对应的List
            string lowerInnerType = innerType.ToLower();

            if (lowerInnerType == "int" || lowerInnerType == "int32")
            {
                List<int> list = new List<int>();
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart) && int.TryParse(trimmedPart, out int intValue))
                    {
                        list.Add(intValue);
                    }
                }
                return list;
            }
            else if (lowerInnerType == "float" || lowerInnerType == "single")
            {
                List<float> list = new List<float>();
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart) && float.TryParse(trimmedPart, out float floatValue))
                    {
                        list.Add(floatValue);
                    }
                }
                return list;
            }
            else if (lowerInnerType == "string" || lowerInnerType == "text")
            {
                List<string> list = new List<string>();
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart))
                    {
                        list.Add(trimmedPart);
                    }
                }
                return list;
            }
            else if (lowerInnerType == "double")
            {
                List<double> list = new List<double>();
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart) && double.TryParse(trimmedPart, out double doubleValue))
                    {
                        list.Add(doubleValue);
                    }
                }
                return list;
            }
            else if (lowerInnerType == "bool" || lowerInnerType == "boolean")
            {
                List<bool> list = new List<bool>();
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart) && bool.TryParse(trimmedPart, out bool boolValue))
                    {
                        list.Add(boolValue);
                    }
                }
                return list;
            }

            // 默认返回空List<string>
            return new List<string>();
        }

        /// <summary>
        /// 解析Vector2字符串
        /// 支持格式：(1,3) 或 1,3
        /// </summary>
        private static Vector2 ParseVector2(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Vector2.zero;

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 2)
            {
                float x = float.Parse(parts[0].Trim());
                float y = float.Parse(parts[1].Trim());
                return new Vector2(x, y);
            }

            Debug.LogWarning($"Vector2格式错误: {value}");
            return Vector2.zero;
        }

        /// <summary>
        /// 解析Vector3字符串
        /// 支持格式：(1,2,3) 或 1,2,3
        /// </summary>
        private static Vector3 ParseVector3(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Vector3.zero;

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 3)
            {
                float x = float.Parse(parts[0].Trim());
                float y = float.Parse(parts[1].Trim());
                float z = float.Parse(parts[2].Trim());
                return new Vector3(x, y, z);
            }

            Debug.LogWarning($"Vector3格式错误: {value}");
            return Vector3.zero;
        }

        /// <summary>
        /// 解析Color字符串
        /// 支持格式：(1,0,0,1) 或 1,0,0,1 (RGBA) 或 (1,0,0) (RGB)
        /// </summary>
        private static Color ParseColor(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Color.white;

            // 移除括号和空格
            value = value.Trim().Replace("(", "").Replace(")", "").Trim();
            string[] parts = value.Split(',');

            if (parts.Length == 4)
            {
                float r = float.Parse(parts[0].Trim());
                float g = float.Parse(parts[1].Trim());
                float b = float.Parse(parts[2].Trim());
                float a = float.Parse(parts[3].Trim());
                return new Color(r, g, b, a);
            }
            else if (parts.Length == 3)
            {
                float r = float.Parse(parts[0].Trim());
                float g = float.Parse(parts[1].Trim());
                float b = float.Parse(parts[2].Trim());
                return new Color(r, g, b, 1.0f);
            }

            Debug.LogWarning($"Color格式错误: {value}");
            return Color.white;
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        private static object GetDefaultValue(string type)
        {
            // 检查是否是List类型
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                // 提取泛型参数 List<int> -> int
                string innerType = type.Substring(5, type.Length - 6).Trim().ToLower();

                // 根据内部类型返回对应的空List
                if (innerType == "int" || innerType == "int32")
                    return new List<int>();
                else if (innerType == "float" || innerType == "single")
                    return new List<float>();
                else if (innerType == "double")
                    return new List<double>();
                else if (innerType == "bool" || innerType == "boolean")
                    return new List<bool>();
                else if (innerType == "string" || innerType == "text")
                    return new List<string>();
                else
                    return new List<string>();
            }

            switch (type.ToLower())
            {
                case "int":
                case "int32":
                case "long":
                case "int64":
                    return 0;
                case "float":
                case "single":
                    return 0f;
                case "double":
                    return 0.0;
                case "bool":
                case "boolean":
                    return false;
                case "vector2":
                    return Vector2.zero;
                case "vector3":
                    return Vector3.zero;
                case "color":
                    return Color.white;
                default:
                    return "";
            }
        }

        /// <summary>
        /// 获取C#类型的默认值字符串表示
        /// </summary>
        public static string GetDefaultValueString(string type)
        {
            // 检查是否是List类型
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                // 提取泛型参数 List<int> -> int
                string innerType = type.Substring(5, type.Length - 6).Trim();
                string convertedInnerType = ConvertToUnityType(innerType);
                return $"new List<{convertedInnerType}>()";
            }

            string lowerType = type.ToLower();

            switch (lowerType)
            {
                case "int":
                case "int32":
                case "long":
                case "int64":
                    return "0";
                case "float":
                case "single":
                    return "0f";
                case "double":
                    return "0.0";
                case "bool":
                case "boolean":
                    return "false";
                case "vector2":
                    return "Vector2.zero";
                case "vector3":
                    return "Vector3.zero";
                case "color":
                    return "Color.white";
                default:
                    return "\"\"";
            }
        }

        /// <summary>
        /// 生成结构列表的ScriptableObject类代码（支持多个结构列表）
        /// </summary>
        public static string GenerateStructListConfigClass(List<ExcelStructListData> structListsData, List<ExcelHorizontalConfigData> paramTablesData, string className)
        {
            StringBuilder sb = new StringBuilder();

            // 添加命名空间和引用
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace ExcelTool");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {className} 配置数据");
            sb.AppendLine("    /// 自动生成，请勿手动修改");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"ExcelTool/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");

            // 添加元数据字段
            sb.AppendLine("        [Header(\"=== 工具元数据（请勿手动修改） ===\")]");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 元数据：Excel文件路径");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [Tooltip(\"此文件由工具自动管理，请勿手动修改\")]");
            sb.AppendLine("        public string _excelFilePath = \"\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 元数据：使用的工作表索引（逗号分隔）");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        [Tooltip(\"此文件由工具自动管理，请勿手动修改\")]");
            sb.AppendLine("        public string _sheetIndices = \"\";");
            sb.AppendLine();
            sb.AppendLine("        [Space(20)]");

            // 生成参数表字段
            if (paramTablesData != null && paramTablesData.Count > 0)
            {
                foreach (var paramTable in paramTablesData)
                {
                    string fieldName = ConvertToCamelCase(SanitizeFieldName(paramTable.sheetName));
                    string nestedClassName = SanitizeClassName(paramTable.sheetName) + "Data";

                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {paramTable.sheetName}");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public {nestedClassName} {fieldName} = new {nestedClassName}();");
                    sb.AppendLine();
                }
            }

            // 生成结构列表字段
            foreach (var structList in structListsData)
            {
                string listFieldName = ConvertToCamelCase(SanitizeFieldName(structList.listName));
                string itemClassName = SanitizeClassName(structList.listName) + "Item";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {structList.sheetName} - {structList.listName}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public List<{itemClassName}> {listFieldName} = new List<{itemClassName}>();");
                sb.AppendLine();
            }

            // 生成参数表的嵌套类
            if (paramTablesData != null && paramTablesData.Count > 0)
            {
                foreach (var paramTable in paramTablesData)
                {
                    string nestedClassName = SanitizeClassName(paramTable.sheetName) + "Data";

                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {paramTable.sheetName} 数据");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine("        [System.Serializable]");
                    sb.AppendLine($"        public class {nestedClassName}");
                    sb.AppendLine("        {");

                    // 生成字段
                    foreach (var field in paramTable.fields)
                    {
                        string fieldName = field.fieldName;
                        string dataType = ConvertToUnityType(field.dataType);
                        string comment = field.comment;
                        string defaultValue = field.value;

                        sb.AppendLine("            /// <summary>");
                        sb.AppendLine($"            /// {comment}");
                        sb.AppendLine("            /// </summary>");

                        string valueString = GetFormattedValue(defaultValue, dataType);
                        sb.AppendLine($"            public {dataType} {fieldName} = {valueString};");
                        sb.AppendLine();
                    }

                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }

            // 生成结构列表的嵌套类
            foreach (var structList in structListsData)
            {
                string itemClassName = SanitizeClassName(structList.listName) + "Item";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {structList.sheetName} - {structList.listName} 数据项");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        [System.Serializable]");
                sb.AppendLine($"        public class {itemClassName}");
                sb.AppendLine("        {");

                // 生成字段
                foreach (var param in structList.parameters)
                {
                    string paramName = param.paramName;
                    string dataType = ConvertToUnityType(param.paramType);
                    string comment = param.comment;

                    sb.AppendLine("            /// <summary>");
                    sb.AppendLine($"            /// {comment}");
                    sb.AppendLine("            /// </summary>");
                    sb.AppendLine($"            public {dataType} {paramName};");
                    sb.AppendLine();
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
