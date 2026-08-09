namespace Contact_Book_MVVM.Models;

// Lưu trữ thông tin đối tượng, chỉ chứa lệnh thuần, khng biết gì về UI
public class Person
{
    // Tên người - ViewModel sẽ gán hoặc đọc qua Binding, không cần INotifyPropertyChanged ở đây, vì danh sách dùng ObservableCollection, nó làm mới item khi sửa
    public string Name { get; set } = "";
    
    // Tuổi kiểu int, textbox bind Age sẽ tự chuển chuỗi khi nhập số hợp lệ
    public int Age { get; set }

    // Số điện thoại - chuổi hển thị trên form và trong danh sách
    public string Phone { get; set; } = "";
    
    // ToString() được ListBox dùng mặt định để hiện mỗi dòng (nếu khôn có ItemTemplate)
    public override string ToString() => $"{Name} | {Age} tuổi | {Phone}";
}