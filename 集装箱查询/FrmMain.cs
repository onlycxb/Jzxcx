#define DEBUG
#undef  DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 集装箱查询
{
    public partial class FrmMain : Form
    {
        private HttpService service = new HttpService();

        public FrmMain()
        {
            InitializeComponent();
            this.txtBookNumer.Text = "SUDU50650A6BP049,SUDU50650A6BP049";
#if DEBUG

            service.showInfo += new Action<List<Info>>(showInfo);
#endif
        }

        /// <summary>
        /// 查询按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSearch_Click(object sender, EventArgs e)
        {
            //更新网页时间
            var url = "https://www.hamburgsud-line.com/linerportal/pages/hsdg/tnt.xhtml";
            var rsp = service.GetHtml(url, "utf-8");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(rsp);

            // 模糊查找起始和终止时间
            var dateFrom = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'inputDateFrom_input')]").GetAttributeValue("value", "");
            var dateTo = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'inputDateTo_input')]").GetAttributeValue("value", "");

            this.Text = $"集装箱查询【{dateFrom} 至 {dateTo}】";

            //查询blNO列表
            var blnoList = new List<string>();                          //blist.Add("SUDU50650A6BP049");
            blnoList = this.txtBookNumer.Text.Split(',').ToList();
            Task.Run(() =>
            {
                List<Info> list = new List<Info>();
                foreach (var bino in blnoList)
                {
                    list.AddRange(service.GetList(bino, dateFrom, dateTo));
                }
                showInfo(list);
            });
        }

        /// <summary>
        /// 显示List<info>
        /// </summary>
        /// <param name="infos"></param>
        private void showInfo(List<Info> infos)
        {
            if (this.dgvInfos.InvokeRequired)
            {
                dgvInfos.Invoke(
                    new Action(() =>
                      {
                          this.dgvInfos.DataSource = null;
                          this.dgvInfos.DataSource = infos;
                      }));
            }
            else
            {
                this.dgvInfos.DataSource = null;
                this.dgvInfos.DataSource = infos;
            }
        }
    }
}