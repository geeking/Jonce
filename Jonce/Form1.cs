using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using DevComponents.Tree;
namespace Jonce
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //用于多线程对UI线程中控件的访问
        delegate void setProgressDelegate(bool obj);
        delegate void setNodeTextDelegate(string text);

        #region 定义各种类型节点的风格
        /// <summary>
        /// 涉及“循环包含”的节点风格
        /// </summary>
        private ElementStyle LoopStyle = new ElementStyle();
        /// <summary>
        /// 涉及“循环包含”的所有父节点的风格
        /// </summary>
        private ElementStyle ParentLoopStyle = new ElementStyle();
        /// <summary>
        /// 未找到文件节点的风格
        /// </summary>
        private ElementStyle MissStyle = new ElementStyle();
        #endregion


        private void button1_Click(object sender, EventArgs e)
        {
            //选取目录
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                clearNodes();
                //开始绘制节点前设置treeGX不可见，否则大量绘制节点并修改节点属性时，会引发"System.InvalidOperationException"错误
                this.treeGX1.Visible = false;
                //设置“加载中”logo的居中显示效果。通过布局控件的自动调整特性，不受窗口大小变化影响。
                tableLayoutPanel2.ColumnStyles[1].SizeType = SizeType.Percent;
                tableLayoutPanel2.ColumnStyles[1].Width = 50F;
                tableLayoutPanel2.ColumnStyles[2].SizeType = SizeType.Absolute;
                tableLayoutPanel2.ColumnStyles[2].Width = 100F;
                //线程启动
                Thread th = new Thread(new ParameterizedThreadStart(doDraw));
                th.Start(fbd.SelectedPath);
            }

        }
        /// <summary>
        /// 用于展示“加载中”
        /// </summary>
        /// <param name="set"></param>
        private void setProgress(bool set)
        {
            //bool set = obj as Boolean;
            this.progressPanel1.Visible = set;
        }
        /// <summary>
        /// 设置Root节点并刷新treeGX
        /// </summary>
        /// <param name="text"></param>
        private void setNodeText(string text)
        {
            this.node1.Text = text;
            this.treeGX1.Refresh();
            //恢复绘制之前的设置
            tableLayoutPanel2.ColumnStyles[1].Width = 0;
            tableLayoutPanel2.ColumnStyles[2].Width = 0;
            this.treeGX1.Visible = true;
        }
        /// <summary>
        /// 创建、设置与添加节点
        /// </summary>
        /// <param name="nodeText"></param>
        /// <param name="parentNode"></param>
        /// <returns></returns>
        private Node creatNode(string nodeText, ref Node parentNode)
        {
            Node node = new Node();
            node.Name = nodeText;
            node.Text = nodeText;
            node.NodeDoubleClick += cNode_NodeDoubleClick;
            parentNode.Nodes.Add(node);
            return node;
        }
        /// <summary>
        /// 绘制处理的主代码
        /// </summary>
        /// <param name="objPath"></param>
        private void doDraw(object objPath)
        {
            setProgressDelegate setProg = new setProgressDelegate(setProgress);
            setNodeTextDelegate setNode = new setNodeTextDelegate(setNodeText);

            string path = objPath as string;
            this.progressPanel1.Invoke(setProg, true);
            //this.progressPanel1.Visible = true;
            CFileHelper fileHelper = new CFileHelper();
            //获取及分析所有C代码文件
            List<CType> cTypeList = fileHelper.GetAllCFile(path);
            //treeGX.Node节点用style
            ElementStyle style = new ElementStyle();
            //节点文字颜色设置
            style.TextColor = Color.Blue;
            for (int i = 0; i < cTypeList.Count; i++)
            {
                CType item = cTypeList[i];
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    //以“.c”文件为绘制主体，分析并递归绘制其中的include关系
                    if (Path.GetExtension(item.FullPath).ToLower() == ".c")
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "\t" + item.FullPath);
                        Node cNode = creatNode(item.FileName, ref this.node1);
                        loopDraw(cTypeList, item.FileName, ref cNode);
                    }
                }
                else
                {
                    if (item.FileName==textBox1.Text)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "\t" + item.FullPath);
                        Node cNode = creatNode(item.FileName, ref this.node1);
                        loopDraw(cTypeList, item.FileName, ref cNode);
                    }
                }
                
            }
            this.treeGX1.Invoke(setNode, path);
            this.progressPanel1.Invoke(setProg, false);

        }
        void cNode_NodeDoubleClick(object sender, EventArgs e)
        {
            Node node = sender as Node;
            if (node.LastNode == null)
            {
                return;
            }
            if (node.Expanded)
            {
                if (checkBox1.Checked)
                {
                    node.CollapseAll();
                }
                else
                {
                    node.Collapse();
                }
            }
            else
            {
                if (checkBox1.Checked)
                {
                    node.ExpandAll();
                }
                else
                {
                    node.Expand();
                }

            }
        }

        /// <summary>
        /// 判断头文件是否循环引用。并设置与循环引用相关节点的显示风格
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool isLoopInclude(string fileName, ref Node node)
        {
            bool retBool = false;
            //Node.FullPath属性有点特别，可在此下断点自行体会
            string[] nodes = node.FullPath.Substring(0, node.FullPath.Length - fileName.Length).Split(';');
            int idx = nodes.Count() - 1;
            int loopCount = 0;
            int allCount = nodes.Count() - 2 + 1;
            for (int i = idx; i >= 0; i--)
            {
                if (nodes[i] == fileName)
                {
                    retBool = true;
                    loopCount = nodes.Count() - i;
                    break;
                }
            }
            if (loopCount > 0)
            {
                initLoopNode(loopCount, allCount, ref node);

            }

            return retBool;
        }
        /// <summary>
        /// 对循环引用相关节点的style进行设置
        /// </summary>
        /// <param name="loopDeep">与循环引用相关的节点个数</param>
        /// <param name="allDeep">节点的总深度</param>
        /// <param name="node">节点</param>
        private void initLoopNode(int loopDeep, int allDeep, ref Node node)
        {
            if (loopDeep >= 0)
            {
                node.Style = LoopStyle;
                Node parNode = node.Parent;
                initLoopNode(loopDeep - 1, allDeep - 1, ref parNode);
            }
            else if (allDeep > 0)
            {
                node.Style = ParentLoopStyle;
                Node parNode = node.Parent;
                initLoopNode(loopDeep - 1, allDeep - 1, ref parNode);
            }
        }

        /// <summary>
        /// 递归绘制指定节点的子节点
        /// </summary>
        /// <param name="cTypeList"></param>
        /// <param name="fileName"></param>
        /// <param name="node"></param>
        private void loopDraw(List<CType> cTypeList, string fileName, ref Node node)
        {
            int tmpCount = 0;

            if (isLoopInclude(fileName, ref node))
            {
                //存在头文件循环包含，则退出当前处理
                return;
            }

            for (tmpCount = 0; tmpCount < cTypeList.Count; tmpCount++)
            {
                CType item = cTypeList[tmpCount];
                if (item.FileName == fileName)
                {
                    //Console.WriteLine(item.FullPath);
                    for (int i = 0; i < item.IncludeList.Count; i++)
                    {
                        string strItem = item.IncludeList[i];

                        Node incNode = creatNode(strItem, ref node);

                        loopDraw(cTypeList, strItem, ref incNode);
                    }
                    break;
                }
            }


            //若没有此文件，则设置其Node相应的属性
            if (tmpCount >= cTypeList.Count)
            {
                node.Style = MissStyle;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                return;
            }
            //DevComponents.Tree.eNodeLayout layout = (DevComponents.Tree.eNodeLayout)Enum.Parse(typeof(DevComponents.Tree.eNodeLayout), comboBox1.SelectedItem.ToString());
            DevComponents.Tree.eMapFlow mapFlow = (DevComponents.Tree.eMapFlow)Enum.Parse(typeof(DevComponents.Tree.eMapFlow), comboBox1.SelectedItem.ToString());
            if (treeGX1.MapLayoutFlow != mapFlow)
            {
                treeGX1.MapLayoutFlow = mapFlow;
                treeGX1.RecalcLayout();
                treeGX1.Refresh();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (this.node1.LastNode == null)
            {
                return;
            }
            if (button2.Text == "ExpandAll")
            {
                this.node1.ExpandAll();
                button2.Text = "CollapseAll";
            }
            else
            {
                foreach (Node item in this.node1.Nodes)
                {
                    item.CollapseAll();
                }
                //this.node1.CollapseAll();
                button2.Text = "ExpandAll";
            }
            treeGX1.Refresh();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            clearNodes();
        }
        private void clearNodes()
        {
            this.node1.Nodes.Clear();
            this.node1.Text = "Created by Geeking";
            treeGX1.Refresh();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG Files | *.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (Bitmap bmp = new Bitmap(treeGX1.TreeSize.Width, treeGX1.TreeSize.Height))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            treeGX1.PaintTo(g, true, Rectangle.Empty);
                        }
                        bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    MessageBox.Show("保存成功");
                }
                catch (Exception)
                {
                    //由于Bitmap(int,int)无法创建太大的图片
                    //实际上，Bitmap(int.MaxValue,int.MaxValue)是出错的，原因暂不清楚，不敢妄断。
                    //为了防止保存图片时程序挂掉，所以加此处理（折中处理）
                    //若有保存大图的需求，还请各位自己想个解决办法。
                    //若解决，有兴趣可以E-mail:geeking09@gmail.com
                    MessageBox.Show("图片过大！", "出错");
                }

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.node1.Text = "Created by Geeking";
            this.progressPanel1.Visible = false;
            //涉及“循环包含”的节点风格
            LoopStyle.TextColor = Color.Red;
            LoopStyle.PaddingLeft = 10;
            LoopStyle.PaddingRight = 10;
            LoopStyle.Font = new Font(Font.Name, 8F);
            LoopStyle.BorderWidth = 1;
            LoopStyle.BorderColor = Color.Red;
            LoopStyle.CornerType = eCornerType.Rounded;
            LoopStyle.Border = eStyleBorderType.Solid;
            //涉及“循环包含”的所有父节点的风格
            ParentLoopStyle.TextColor = Color.Fuchsia;
            ParentLoopStyle.PaddingLeft = 10;
            ParentLoopStyle.PaddingRight = 10;
            ParentLoopStyle.BorderWidth = 1;
            ParentLoopStyle.BorderColor = Color.Fuchsia;
            ParentLoopStyle.CornerType = eCornerType.Rounded;
            ParentLoopStyle.Border = eStyleBorderType.Solid;
            //未找到文件节点的风格
            MissStyle.TextColor = Color.Orange;
            MissStyle.PaddingLeft = 10;
            MissStyle.PaddingRight = 10;
            MissStyle.BorderWidth = 1;
            MissStyle.BorderColor = Color.Orange;
            MissStyle.CornerType = eCornerType.Diagonal;
            MissStyle.Border = eStyleBorderType.Dot;
            //去除“记载中”logo的占位
            tableLayoutPanel2.ColumnStyles[1].Width = 0;
            tableLayoutPanel2.ColumnStyles[2].Width = 0;

        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            treeGX1.Zoom = (float)trackBar1.Value / 100;
            label1.Text = trackBar1.Value.ToString() + "%";
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void label1_DoubleClick(object sender, EventArgs e)
        {
            trackBar1.Value = 100;
        }
    }
}
