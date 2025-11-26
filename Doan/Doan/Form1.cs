using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Doan
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            KhoiTaoGioiTinh();
            KhoiTaoMonHoc();
            LoadData();

            // Gán sự kiện để tự động in hoa khi nhập
            txtMaSV.TextChanged += TxtToUpper;
            txtHoTen.TextChanged += TxtToUpper;
            txtLop.TextChanged += TxtToUpper;
        }

        //HAM IN HOA
        private void TxtToUpper(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            int viTriConTro = txt.SelectionStart;
            txt.Text = txt.Text.ToUpper();
            txt.SelectionStart = viTriConTro; // Giữ nguyên vị trí con trỏ khi gõ
        }

        // --------------------- LOAD DỮ LIỆU TỪ MYSQL ---------------------
        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT sv.MaSV, sv.HoTen, sv.Lop, sv.NgaySinh, sv.GioiTinh, 
                                            d.TenMon, d.DiemQT, d.DiemThi, d.DiemTB
                                     FROM SINHVIEN sv
                                     LEFT JOIN DIEM d ON sv.MaSV = d.MaSV";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvBangDuLieu.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
            }
        }

        // --------------------- HÀM KHỞI TẠO ---------------------
        private void KhoiTaoGioiTinh()
        {
            cbGioiTinh.Items.Clear();
            cbGioiTinh.Items.Add("Nam");
            cbGioiTinh.Items.Add("Nữ");
            cbGioiTinh.SelectedIndex = 0;
        }

        private void KhoiTaoMonHoc()
        {
            cbMonHoc.Items.Clear();
            cbMonHoc.Items.Add("Cấu trúc dữ liệu");
            cbMonHoc.Items.Add("Lập trình C#");
            cbMonHoc.Items.Add("Cơ sở dữ liệu");
            cbMonHoc.Items.Add("Mạng máy tính");
            cbMonHoc.Items.Add("Toán rời rạc");
            cbMonHoc.SelectedIndex = 0;
        }

        // --------------------- CÁC HÀM GIỮ NGUYÊN ---------------------
        private void label1_Click(object sender, EventArgs e) { }
        private void txtMaSV_TextChanged(object sender, EventArgs e) { }
        private void cbNgaySinh_SelectedIndexChanged(object sender, EventArgs e) { }

        private void btnTimKiemvaSua_Click(object sender, EventArgs e) 
        {
            // Truyền optional MaSV nếu muốn tự điền vào Form2
            string maSV = txtMaSV.Text.Trim();
            Form2 frm2 = new Form2(maSV); // Form2 có constructor nhận MaSV
            frm2.ShowDialog(); // mở Form2 dưới dạng modal
            LoadData(); // tải lại dữ liệu sau khi sửa xong

        }

        // --------------------- NÚT THÊM ---------------------
        private void btnThem_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra nhập liệu
                if (string.IsNullOrWhiteSpace(txtMaSV.Text) ||
                    string.IsNullOrWhiteSpace(txtHoTen.Text) ||
                    string.IsNullOrWhiteSpace(txtLop.Text) ||
                    string.IsNullOrWhiteSpace(txtDiemQT.Text) ||
                    string.IsNullOrWhiteSpace(txtDiemThi.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo");
                    return;
                }

                float diemQT = float.Parse(txtDiemQT.Text);
                float diemThi = float.Parse(txtDiemThi.Text);
                float diemTB = (diemQT + diemThi) / 2;

                using (var connCheck = Database.GetConnection())
                {
                    connCheck.Open();
                    string sqlCheck = @"SELECT COUNT(*) FROM DIEM 
                                WHERE MaSV = @MaSV AND TenMon = @TenMon";

                    using (MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, connCheck))
                    {
                        cmdCheck.Parameters.AddWithValue("@MaSV", txtMaSV.Text.Trim());
                        cmdCheck.Parameters.AddWithValue("@TenMon", cbMonHoc.SelectedItem.ToString());

                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Sinh viên này đã có điểm môn học này rồi!",
                                            "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return; //  DỪNG KHÔNG CHÈN
                        }
                    }
                }

                    using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    MySqlTransaction tran = conn.BeginTransaction();

                    try
                    {
                        // 1. Thêm hoặc cập nhật sinh viên
                        string sqlSV = @"INSERT INTO SINHVIEN (MaSV, HoTen, Lop, NgaySinh, GioiTinh)
                                         VALUES (@MaSV, @HoTen, @Lop, @NgaySinh, @GioiTinh)
                                         ON DUPLICATE KEY UPDATE HoTen=@HoTen, Lop=@Lop, NgaySinh=@NgaySinh, GioiTinh=@GioiTinh";

                        using (MySqlCommand cmd = new MySqlCommand(sqlSV, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@MaSV", txtMaSV.Text.Trim());
                            cmd.Parameters.AddWithValue("@HoTen", txtHoTen.Text.Trim());
                            cmd.Parameters.AddWithValue("@Lop", txtLop.Text.Trim());
                            cmd.Parameters.AddWithValue("@NgaySinh", dtpNgaySinh.Value);
                            cmd.Parameters.AddWithValue("@GioiTinh", cbGioiTinh.SelectedItem.ToString());
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Thêm điểm
                        string sqlDiem = @"INSERT INTO DIEM (MaSV, TenMon, DiemQT, DiemThi, DiemTB)
                                           VALUES (@MaSV, @TenMon, @DiemQT, @DiemThi, @DiemTB)
                                           ON DUPLICATE KEY UPDATE DiemQT=@DiemQT, DiemThi=@DiemThi, DiemTB=@DiemTB";

                        using (MySqlCommand cmd2 = new MySqlCommand(sqlDiem, conn, tran))
                        {
                            cmd2.Parameters.AddWithValue("@MaSV", txtMaSV.Text.Trim());
                            cmd2.Parameters.AddWithValue("@TenMon", cbMonHoc.SelectedItem.ToString());
                            cmd2.Parameters.AddWithValue("@DiemQT", diemQT);
                            cmd2.Parameters.AddWithValue("@DiemThi", diemThi);
                            cmd2.Parameters.AddWithValue("@DiemTB", diemTB);
                            cmd2.ExecuteNonQuery();
                        }

                        tran.Commit();
                        MessageBox.Show("Thêm dữ liệu thành công!", "Thông báo");
                        LoadData();
                        txtDiemQT.Clear();
                        txtDiemThi.Clear();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        MessageBox.Show("Điểm nhập không hợp lệ! Điểm từ 0->10", "",
                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        // --------------------- NÚT XÓA ---------------------
        private void btnXoa_Click(object sender, EventArgs e)
        {
            if (dgvBangDuLieu.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một dòng để xóa!", "Thông báo");
                return;
            }

            string maSV = dgvBangDuLieu.SelectedRows[0].Cells["MaSV"].Value.ToString();

            DialogResult result = MessageBox.Show($"Bạn có chắc muốn xóa sinh viên {maSV} không?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var conn = Database.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM SINHVIEN WHERE MaSV = @MaSV";
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaSV", maSV);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Xóa thành công!", "Thông báo");
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                }
            }
        }

        // --------------------- NÚT NHẬP MỚI ---------------------
        private void btnNhapTTmoi_Click(object sender, EventArgs e)
        {
            txtMaSV.Clear();
            txtHoTen.Clear();
            txtLop.Clear();
            txtDiemQT.Clear();
            txtDiemThi.Clear();
            cbGioiTinh.SelectedIndex = 0;
            cbMonHoc.SelectedIndex = 0;
            dtpNgaySinh.Value = DateTime.Now;
            txtMaSV.Focus();
        }

        // --------------------- NÚT THOÁT ---------------------
        private void btnThoat_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show("Bạn có chắc muốn thoát không?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
                this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    // Lấy dữ liệu: mỗi dòng là 1 môn của 1 sinh viên
                    string query = @"
                SELECT sv.MaSV, sv.HoTen, sv.Lop, sv.NgaySinh, sv.GioiTinh,
                       d.TenMon, d.DiemQT, d.DiemThi, d.DiemTB
                FROM SINHVIEN sv
                LEFT JOIN DIEM d ON sv.MaSV = d.MaSV
                ORDER BY sv.MaSV, d.TenMon";

                    DataTable dt = new DataTable();
                    using (MySqlDataAdapter ad = new MySqlDataAdapter(query, conn))
                    {
                        ad.Fill(dt);
                    }

                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("DanhSach");

                        // Header
                        ws.Cell(1, 1).Value = "MaSV";
                        ws.Cell(1, 2).Value = "HoTen";
                        ws.Cell(1, 3).Value = "Lop";
                        ws.Cell(1, 4).Value = "NgaySinh";
                        ws.Cell(1, 5).Value = "GioiTinh";
                        ws.Cell(1, 6).Value = "TenMon";
                        ws.Cell(1, 7).Value = "DiemQT";
                        ws.Cell(1, 8).Value = "DiemThi";
                        ws.Cell(1, 9).Value = "DiemTB";

                        int row = 2;
                        string lastMaSV = null;

                        foreach (DataRow r in dt.Rows)
                        {
                            string ma = r["MaSV"]?.ToString() ?? "";

                            bool isNewStudent = ma != lastMaSV;

                            if (isNewStudent)
                            {
                                // Ghi thông tin sinh viên trên dòng đầu môn đầu tiên
                                ws.Cell(row, 1).Value = ma;
                                ws.Cell(row, 2).Value = r["HoTen"]?.ToString() ?? "";
                                ws.Cell(row, 3).Value = r["Lop"]?.ToString() ?? "";

                                if (r["NgaySinh"] != DBNull.Value && DateTime.TryParse(r["NgaySinh"].ToString(), out DateTime ns))
                                    ws.Cell(row, 4).Value = ns.ToString("dd/MM/yyyy");
                                else
                                    ws.Cell(row, 4).Value = "";

                                ws.Cell(row, 5).Value = r["GioiTinh"]?.ToString() ?? "";
                            }
                            else
                            {
                                // Các dòng môn tiếp theo để trống các cột thông tin sinh viên
                                ws.Cell(row, 1).Value = "";
                                ws.Cell(row, 2).Value = "";
                                ws.Cell(row, 3).Value = "";
                                ws.Cell(row, 4).Value = "";
                                ws.Cell(row, 5).Value = "";
                            }

                            // Ghi môn và điểm (mỗi môn 1 dòng)
                            ws.Cell(row, 6).Value = r["TenMon"]?.ToString() ?? "";

                            // Ghi điểm thẳng vào từng cột (không xuống dòng trong ô) — phù hợp yêu cầu "các cột điểm ngang ngang"
                            ws.Cell(row, 7).Value = r["DiemQT"] != DBNull.Value ? r["DiemQT"].ToString() : "";
                            ws.Cell(row, 8).Value = r["DiemThi"] != DBNull.Value ? r["DiemThi"].ToString() : "";
                            ws.Cell(row, 9).Value = r["DiemTB"] != DBNull.Value ? r["DiemTB"].ToString() : "";

                            lastMaSV = ma;
                            row++;
                        }

                        // Tự động điều chỉnh kích thước cột
                        ws.Columns().AdjustToContents();

                        // Lưu file
                        SaveFileDialog sd = new SaveFileDialog();
                        sd.Filter = "Excel File (*.xlsx)|*.xlsx";
                        sd.FileName = "DanhSachSinhVien_Mon_TheoDong.xlsx";

                        if (sd.ShowDialog() == DialogResult.OK)
                        {
                            wb.SaveAs(sd.FileName);
                            MessageBox.Show("Xuất file Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();  // Tạo form 3
            f3.ShowDialog();
        }
    }
}
