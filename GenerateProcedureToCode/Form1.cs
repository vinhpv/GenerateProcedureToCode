using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GenerateProcedureToCode
{
    public partial class Form1 : Form
    {
        bool isFunc = false;
        string type = "DataTable", name = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string s = txtInput.Text;
            // chuẩn hóa loại dấu cách trắng và dấu xuống dòng
            s = Regex.Replace(s, @"\s+", " ", RegexOptions.Multiline).ToLower().Trim();
            var ss = s.Split(' ');
            if (ss.Count() < 2)
            {
                MessageBox.Show("Chuỗi không đúng định dạng, bạn kiểm tra lại"); return;
            }
            // Tìm xem là function hay proc
            txtProc.Text = ss.First();
            // tên proc
            txtName.Text = name = ss[1];
            // Tìm type return
            if (txtProc.Text == "function")
            {
                isFunc = true;
                type = Parameter.getTypeNET(ss.Last());
            }
            // Tìm các parametor
            List<Parameter> pras = new List<Parameter>();
            var math = Regex.Matches(s, @"\((.+?)\)");
            string param = "";
            if (math.Count > 0)
                param = math[0].Value.Replace("(", "").Replace(")", "");
            param.Split(',').ToList().ForEach((p) =>
            {
                var pp = p.Trim().Split(' ');
                pras.Add(new Parameter() { paraName = pp.First(), paraOraceType = pp.Last() });
            });
            if (!isFunc && pras.Where(p => p.paraOraceType.Contains("cursor")).Count() == 0)
                type = "void";
            txtType.Text = type;

            //ghép kết quả thành chuỗi code
            string rs = @"public " + type + " " + name + "(";
            string thamso = "", thamsoOra = "";
            if (isFunc)
            {
                thamsoOra = @"
                new OracleParameter() { ParameterName = ""return_value"", OracleDbType = OracleDbType." + Parameter.getOracleDBType(type) + @",Direction = ParameterDirection.ReturnValue, Size = 4000},";
            }
            pras.ForEach((p) =>
            {
                thamso += p.paraType + " " + p.paraName + ",";
                if (p.paraOraceType.Contains("cursor"))
                {
                    thamsoOra += @"
                        new OracleParameter(""" + p.paraName + @""",OracleDbType.RefCursor,ParameterDirection.Output),
                    ";
                }
                else
                {
                    thamsoOra += @"
                               new OracleParameter() {ParameterName = """ + p.paraName + @""",OracleDbType = OracleDbType." + p.oraceType + @", Value = " + p.paraName + @"},";
                }
            });
            rs += thamso.Remove(thamso.Length - 1) + " )";// loại dấu , cuối
            rs += @"
            {
                string str = """";
                str = DatabaseConstants.DB2 + ""." + name + @""";
                OracleParameter[] parms;
                parms = new OracleParameter[]
                                {" + thamsoOra + @"
                };";
            if (type == "void")
            {
                rs += @"
                    OracleDataAccess.ExecuteNonQuery(CommandType.StoredProcedure, str, parms);
         }
                ";
            }
            else if (type == "DataTable")
            {
                rs += @"
                    return OracleDataAccess.getDataFromProcedure(str, ""table"", parms).Tables[0];
         }
                ";
            }
            else
            {
                rs += @"
                    OracleDataAccess.ExecuteNonQuery(CommandType.StoredProcedure, str, parms);
                    Return parms[0].Value as " + type + @"
         }
                ";
            }
            /*
                    public void thaydoi_ttkn(long vkhieunai_id, int vnhanvien_id, int vdonvi_id, int vtrangthai_id,
            string vghichu, string vdienthoai_lh, string vnd_khieunai, string vnoidung_gq, string vnguyennhan_kn,
            decimal vgiamcuoc_dt, decimal vvat_dt, decimal vgiamcuoc_cp, decimal vvat_cp, DateTime vngay_cn,
            string vnguoi_cn, string vmay_cn, string vip_cn
            )
        {
            string str = "";
            str = DatabaseConstants.DB3 + ".thaydoi_ttkn";
            OracleParameter[] parms;
            parms = new OracleParameter[] 
							{
                                new OracleParameter("vkhieunai_id", OracleDbType.Int64),
                                new OracleParameter("vngay_gq", OracleDbType.Date),
                                new OracleParameter("vnhanvien_gq_id", OracleDbType.Int32),
                                new OracleParameter("vnoidung_gq", OracleDbType.Varchar2),
                                new OracleParameter("vnd_ton", OracleDbType.Varchar2),
                                new OracleParameter("vnguyennhan", OracleDbType.Varchar2),
                                new OracleParameter("vtien_gc", OracleDbType.Decimal),
                                new OracleParameter("vvat_gc", OracleDbType.Decimal),
			};
            parms[0].Value = vkhieunai_id;
            parms[1].Value = vngay_gq;
            parms[2].Value = vnhanvien_gq_id;
            parms[3].Value = vnoidung_gq;
            parms[4].Value = vnd_ton;
            parms[5].Value = vnguyennhan;
            parms[6].Value = vtien_gc;
            parms[7].Value = vvat_gc;
            OracleDataAccess.ExecuteNonQuery(CommandType.StoredProcedure, str, parms);
        }
            */
            txtOutput.Text = rs;
        }

        internal class Parameter
        {
            public string paraName { get; set; }
            public string paraOraceType { get; set; }
            public string parameterDirection { get; set; }
            public string paraType
            {
                get
                {
                    return getTypeNET(paraOraceType);
                }
            }
            public string oraceType
            {
                get
                {
                    return getOracleType(paraOraceType);
                }
            }
            public static string getTypeNET(string OraceType)
            {
                string s = "";
                switch (OraceType)
                {
                    case "number":
                        s = "decimal";
                        break;
                    case "date":
                        s = "DateTime";
                        break;
                    default:
                        s = "string";
                        break;
                }
                return s;
            }
            public static string getOracleType(string paraOraceType)
            {
                string s = "";
                switch (paraOraceType)
                {
                    case "number":
                        s = "Decimal";
                        break;
                    case "date":
                        s = "Date";
                        break;
                    default:
                        s = "Varchar2";
                        break;
                }
                return s;
            }
            public static string getOracleDBType(string type)
            {
                string s = "";
                switch (type)
                {
                    case "decimal":
                        s = "Decimal";
                        break;
                    case "DataTime":
                        s = "Date";
                        break;
                    default:
                        s = "Varchar2";
                        break;
                }
                return s;
            }
        }
    }
}
