using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Jonce
{
    class CFileHelper
    {
        private List<string> fileList = new List<string>();
        /// <summary>
        /// 获取及分析所有C代码文件
        /// </summary>
        /// <param name="path">C项目路径</param>
        /// <returns></returns>
        public List<CType> GetAllCFile(string path)
        {
            List<CType> retList = new List<CType>();

            getAllByPath(path);
            //过滤出所有文件中的代码文件
            //分析引用，并存入List<CType>结构内
            foreach (string item in fileList)
            {
                string extension = Path.GetExtension(item).ToLower();
                if (extension == ".c" || extension == ".h" || extension == ".cpp")
                {
                    CType cType = new CType();
                    cType.FullPath = item;
                    cType.FileName = Path.GetFileName(item);
                    //获取C文件中include引用的头文件
                    cType.IncludeList = SourceHelper.GetIncludeFile(SourceHelper.RemoveComments(item));
                    retList.Add(cType);
                }
            }

            return retList;
        }
        //获取指定目录下的所有文件
        private void getAllByPath(string path)
        {
            if (path.EndsWith("\\"))
            {
                fileList.Add(path);
            }
            else
            {
                fileList.Add(path + "\\");
            }

            string[] dirs = Directory.GetDirectories(path);
            fileList.AddRange(Directory.GetFiles(path));
            foreach (string dir in dirs)
            {
                getAllByPath(dir.ToString());
            }
        }
    }
}
