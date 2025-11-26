    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ClosedXML.Excel;
    using System.IO;

namespace Doan
    {
        public partial class Form3 : Form
        {
            public Form3()
            {
                InitializeComponent();
                this.Load += Form3_Load;
                this.cbMonHoc.SelectedIndexChanged += new System.EventHandler(this.cbMonHoc_SelectedIndexChanged);
                
            }

        private void Form3_Load(object sender, EventArgs e)
        {
            LoadMonHoc();
        }

        private void LoadMonHoc()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT TenMon FROM DIEM ORDER BY TenMon";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    using (MySqlDataReader rd = cmd.ExecuteReader())
                    {
                        cbMonHoc.Items.Clear();
                        while (rd.Read())
                        {
                            cbMonHoc.Items.Add(rd.GetString("TenMon"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải môn học: " + ex.Message);
            }
        }

       

        private void LoadSinhVienTheoMon(string mon)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                SELECT sv.MaSV, sv.HoTen, sv.Lop,
                       d.DiemQT, d.DiemThi, d.DiemTB
                FROM SINHVIEN sv
                JOIN DIEM d ON sv.MaSV = d.MaSV
                WHERE d.TenMon = @mon";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@mon", mon);

                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dgvDanhSach.DataSource = dt;
                       

                        // ---- CẤU HÌNH DATAGRIDVIEW CHUẨN ----
                        dgvDanhSach.AllowUserToAddRows = false;
                        dgvDanhSach.ReadOnly = true;

                        dgvDanhSach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        dgvDanhSach.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                        dgvDanhSach.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        dgvDanhSach.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);

                        dgvDanhSach.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                        dgvDanhSach.DefaultCellStyle.Font = new Font("Segoe UI", 10);

                        dgvDanhSach.EnableHeadersVisualStyles = false;
                        dgvDanhSach.ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray;
                        dgvDanhSach.DefaultCellStyle.BackColor = Color.White;

                        dgvDanhSach.BorderStyle = BorderStyle.None;
                        dgvDanhSach.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

                        dgvDanhSach.RowHeadersVisible = false;

                        // Đặt lại tên cột
                        dgvDanhSach.Columns["MaSV"].HeaderText = "Mã SV";
                        dgvDanhSach.Columns["HoTen"].HeaderText = "Họ Tên";
                        dgvDanhSach.Columns["Lop"].HeaderText = "Lớp";
                        dgvDanhSach.Columns["DiemQT"].HeaderText = "Điểm QT";
                        dgvDanhSach.Columns["DiemThi"].HeaderText = "Điểm Thi";
                        dgvDanhSach.Columns["DiemTB"].HeaderText = "Điểm TB";

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void cbMonHoc_SelectedIndexChanged(object sender, EventArgs e)
            {
            if (cbMonHoc.SelectedIndex != -1)
            {
                LoadSinhVienTheoMon(cbMonHoc.Text);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }

        private void btnXuatFile_Click(object sender, EventArgs e)
        {
            if (dgvDanhSach.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel File (*.xlsx)|*.xlsx";
            save.FileName = "DanhSachSinhVien_" + cbMonHoc.Text + ".xlsx";

            if (save.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        DataTable dt = new DataTable();

                        // Tạo cột
                        foreach (DataGridViewColumn col in dgvDanhSach.Columns)
                        {
                            dt.Columns.Add(col.HeaderText);
                        }

                        // Đổ dữ liệu
                        foreach (DataGridViewRow row in dgvDanhSach.Rows)
                        {
                            dt.Rows.Add(
                                row.Cells["MaSV"].Value,
                                row.Cells["HoTen"].Value,
                                row.Cells["Lop"].Value,
                                row.Cells["DiemQT"].Value,
                                row.Cells["DiemThi"].Value,
                                row.Cells["DiemTB"].Value
                            );
                        }

                        // Tạo sheet
                        var ws = wb.Worksheets.Add(dt, "DanhSach");

                        // Canh cột đẹp
                        ws.Columns().AdjustToContents();

                        wb.SaveAs(save.FileName);
                    }

                    MessageBox.Show("Xuất file Excel thành công!", "Thành công");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xuất Excel: " + ex.Message);
                }
            }
        }
    }
}
