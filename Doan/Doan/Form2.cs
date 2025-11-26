using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Doan
{
    public partial class Form2 : Form
    {
        private string initialMaSV = ""; // Lưu MaSV từ Form1

        public Form2(string maSV = "")
        {
            InitializeComponent();
            txtMaSV.TextChanged += TxtToUpper;
            initialMaSV = maSV;
        }

        // ------------------ FORM LOAD ------------------
        private void Form2_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(initialMaSV))
            {
                txtMaSV.Text = initialMaSV;
                TimKiem(initialMaSV);
            }
            else
            {
                LoadData();
            }
        }

        // ------------------ HÀM IN HOA ------------------
        private void TxtToUpper(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null) return;

            txt.TextChanged -= TxtToUpper;
            int pos = txt.SelectionStart;
            txt.Text = txt.Text.ToUpper();
            txt.SelectionStart = pos;
            txt.TextChanged += TxtToUpper;
        }

        // ------------------ NẠP DỮ LIỆU ------------------
        private void LoadData()
        {
            try
            {
                MySqlConnection conn = Database.GetConnection();
                conn.Open();

                string query = @"SELECT sv.MaSV, sv.HoTen, sv.Lop, d.TenMon, d.DiemQT, d.DiemThi,
                                ROUND((d.DiemQT + d.DiemThi)/2,2) AS DiemTB
                                FROM SINHVIEN sv
                                LEFT JOIN DIEM d ON sv.MaSV = d.MaSV";

                MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvBangDuLieu.DataSource = dt;
                LockColumns();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
            }
        }

        // ------------------ TÌM KIẾM ------------------
        private void TimKiem(string maSV)
        {
            try
            {
                MySqlConnection conn = Database.GetConnection();
                conn.Open();

                string query = @"SELECT sv.MaSV, sv.HoTen, sv.Lop, d.TenMon, d.DiemQT, d.DiemThi,
                                ROUND((d.DiemQT + d.DiemThi)/2,2) AS DiemTB
                                FROM SINHVIEN sv
                                LEFT JOIN DIEM d ON sv.MaSV = d.MaSV
                                WHERE sv.MaSV = @MaSV";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MaSV", maSV.Trim());
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    dgvBangDuLieu.DataSource = dt;
                    LockColumns();   // <<< ĐÚNG VỊ TRÍ
                }
                else
                {
                    MessageBox.Show("Không tìm thấy sinh viên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            string maSV = txtMaSV.Text.Trim();
            if (!string.IsNullOrEmpty(maSV))
                TimKiem(maSV);
            else
                LoadData();
        }

        // ------------------ SỬA ------------------
        private void btnSua_Click(object sender, EventArgs e)
        {
            if (dgvBangDuLieu.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một dòng để sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                DataGridViewRow row = dgvBangDuLieu.SelectedRows[0];
                string maSV = row.Cells["MaSV"].Value.ToString().ToUpper();
                string hoTen = row.Cells["HoTen"].Value.ToString().ToUpper();
                string lop = row.Cells["Lop"].Value.ToString().ToUpper();
                string tenMon = row.Cells["TenMon"].Value.ToString().ToUpper();

                float diemQT = float.Parse(row.Cells["DiemQT"].Value.ToString());
                float diemThi = float.Parse(row.Cells["DiemThi"].Value.ToString());

                if (diemQT < 0 || diemQT > 10 || diemThi < 0 || diemThi > 10)
                {
                    MessageBox.Show("Điểm phải trong khoảng 0 - 10!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MySqlConnection conn = Database.GetConnection();
                conn.Open();

                // Cập nhật bảng SINHVIEN
                string querySV = @"UPDATE SINHVIEN SET HoTen=@HoTen, Lop=@Lop WHERE MaSV=@MaSV";
                MySqlCommand cmd1 = new MySqlCommand(querySV, conn);
                cmd1.Parameters.AddWithValue("@HoTen", hoTen);
                cmd1.Parameters.AddWithValue("@Lop", lop);
                cmd1.Parameters.AddWithValue("@MaSV", maSV);
                cmd1.ExecuteNonQuery();

                // Cập nhật hoặc thêm mới DIEM
                string checkQuery = "SELECT COUNT(*) FROM DIEM WHERE MaSV=@MaSV AND TenMon=@TenMon";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@MaSV", maSV);
                checkCmd.Parameters.AddWithValue("@TenMon", tenMon);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    string updateQuery = "UPDATE DIEM SET DiemQT=@DiemQT, DiemThi=@DiemThi, DiemTB=(@DiemQT+@DiemThi)/2 WHERE MaSV=@MaSV AND TenMon=@TenMon";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@MaSV", maSV);
                    updateCmd.Parameters.AddWithValue("@TenMon", tenMon);
                    updateCmd.Parameters.AddWithValue("@DiemQT", diemQT);
                    updateCmd.Parameters.AddWithValue("@DiemThi", diemThi);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    string insertQuery = "INSERT INTO DIEM(MaSV, TenMon, DiemQT, DiemThi, DiemTB) VALUES(@MaSV,@TenMon,@DiemQT,@DiemThi,(@DiemQT+@DiemThi)/2)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@MaSV", maSV);
                    insertCmd.Parameters.AddWithValue("@TenMon", tenMon);
                    insertCmd.Parameters.AddWithValue("@DiemQT", diemQT);
                    insertCmd.Parameters.AddWithValue("@DiemThi", diemThi);
                    insertCmd.ExecuteNonQuery();
                }

                MessageBox.Show("Cập nhật thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
               
                TimKiem(maSV);
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------ THOÁT ------------------
        private void btnThoat_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ------------------ LABEL ------------------
        private void label1_Click(object sender, EventArgs e) { } // giữ lại tránh lỗi Designer

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void LockColumns()
        {
            if (dgvBangDuLieu.Columns.Contains("MaSV"))
                dgvBangDuLieu.Columns["MaSV"].ReadOnly = true;

            if (dgvBangDuLieu.Columns.Contains("Lop"))
                dgvBangDuLieu.Columns["Lop"].ReadOnly = true;

            if (dgvBangDuLieu.Columns.Contains("TenMon"))
                dgvBangDuLieu.Columns["TenMon"].ReadOnly = true;
          
            if (dgvBangDuLieu.Columns.Contains("NgaySinh"))
                dgvBangDuLieu.Columns["NgaySinh"].ReadOnly = true;

            dgvBangDuLieu.AllowUserToAddRows = false;
        }


    }
}
