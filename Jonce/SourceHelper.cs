using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
namespace Jonce
{
    class SourceHelper
    {
        /// <summary>
        /// 去掉代码文件中的注释
        /// </summary>
        /// <param name="filePath">文件全路径</param>
        /// <returns>文件前1M内容（去掉注释）</returns>
        public static string RemoveComments(string filePath)
        {
            string retStr = "";
            //1M缓冲区
            char[] buffer = new char[1024 * 1024];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.Default))
                {
                    try
                    {
                        //string fileStr = sr.ReadToEnd();
                        //读取文件。只读取<=1M内容
                        sr.Read(buffer, 0, buffer.Length);
                        //字符数组转换为字符串，进行正则匹配
                        string fileStr = new string(buffer);
                        //正则表达式，匹配多行注释和单行注释
                        string regStr = @"/\*[\s\S]*?\*/|//.*";
                        //去掉多行注释
                        retStr = Regex.Replace(fileStr, regStr, "");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "ERR");
                    }

                }

            }
            return retStr;
        }
        /// <summary>
        /// 获取C文件中include引用的头文件
        /// </summary>
        /// <param name="fileStr">文件全路径</param>
        /// <returns>头文件List集合</returns>
        public static List<string> GetIncludeFile(string fileStr)
        {
            List<string> headFileList = new List<string>();
            //匹配include语句
            string regStr1 = @"#\s*include\s*(""|<).*";
            //匹配头文件
            string regStr2 = @"\w+\.(h|H)\b";

            Match mc1 = Regex.Match(fileStr, regStr1);
            while (mc1.Success)
            {
                Match mc2 = Regex.Match(mc1.ToString(), regStr2);
                if (mc2.Success)
                {
                    headFileList.Add(mc2.ToString());
                }
                mc1 = mc1.NextMatch();
            }
            return headFileList;
        }
    }
}
