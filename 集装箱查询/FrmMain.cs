#define DEBUG
#undef  DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 集装箱查询
{
    public partial class FrmMain : Form
    {
        HttpService service = new HttpService();
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
            //设置查询起始时间
            string dateFrom = "14-Aug-2020", dateTo = "24-Oct-2020";

            //查询blNO列表
            var blnoList = new List<string>();                          //blist.Add("SUDU50650A6BP049");
            blnoList = this.txtBookNumer.Text.Split(',').ToList();
            Task.Run(() =>
            {

                //用于存放结果的集合
                List<Info> list = new List<Info>();
                //循环查询
                foreach (var bino in blnoList)
                {
                    list.AddRange(service.GetList(bino, dateFrom, dateTo));
                }

                if (this.dgvInfos.InvokeRequired)
                    this.dgvInfos.Invoke(new Action(() => this.dgvInfos.DataSource = list));
                else
                    this.dgvInfos.DataSource = list;
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
