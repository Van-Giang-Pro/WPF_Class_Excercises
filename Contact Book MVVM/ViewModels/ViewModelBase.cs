using System.ComponentModel; // Cung cấp interface INotifyPropertyChanged và sự kiện PropertyChangedEventHandler
using System.Runtime.CompilerServices; // Cung cấp Attribute kỳ diệu tên là [CallerMemberName] tự động điền tên property trên property changed

namespace Contact_Book_MVVM.ViewModels;

// Triển khai interface, lớp này cam kết triển khai cái của INotìyPropertyChanged
public abstract class ViewModelBase : INotifyPropertyChanged // Ta có abstract thì lớp này sinh ra để lớp khác kế thừa
{
    public event PropertyChangedEventHandler? PropertyChanged;
    // Đăng ký sự kiện, dấu chấm hỏi là khi tạo ra chưa có ai đăng ký nghe nên nó có thể null
    // Cái PropertyChangedEventHandler là cá delegate đã được định nghĩa sẵn trong dot net, đòi hỏi mọi handler (phương thức sẽ đăng ký) phải có chữ ký void Handler(object? sender, PropertyChangedEventArgs e)
    // PropertyChanged là tên của kiện, đây là tên chuẩn mà giao diện INotifyPropertyChanged yêu cầu
    // Ta có event là từ khóa khai báo sự kiện
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    // Ta có protected là chỉ cho phép lớp ViewModelBase và các lớp con kế thừa nó (như MainViewModel) được gọi hàm này
    // Cái nào gọi OnPropertyChanged thì nó sẽ điền name là tên thuộc tính đó, sau đó cái nào gắn event PropertyChanged nó sẽ
    // Invoke báo cho giao diện xaml để thay đổi, từ khóa new là bắn tín hiêu thay đổi kèm theo tên thuộc tính vừa thay đổi

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) // giống nhau thì bỏ qua
            return false;
        // Gán giá trị mới vào field private
        field = value;
        // Báo Binding là thuộc tính name đã đổi với textbox, textblock, listbox cập nhật
        OnPropertyChanged(name);
        return true;
    }
    // Ta có ref T field là tham chiếu tới biến private field mà bạn muốn gán giá trị mới
    // Còn T value là giá trị mới muốn gán vào field
    // Ta có <T> là định nghĩa kiểu generic, khi gọi hàm trình biên dịch sẽ thay T bằng kiểu dữ liệu thực tế
    // Với SetProperty<T> là tên hàm với generic type T cho phép hàm dùng được mọi kiểu dữ liệu
    // <T> chỉ định nghĩa kiểu của các tham số (ref T field, T value), khi bạn gọi SetProperty(ref _age, value), T sẽ được suy ra là int
    // Ta có Attribute [CallerMemberName] là khi hàm được gọi mà không truyền đối số name, trình biên dịch sẽ tự động điền tên của thành viên đang gọi hàm (ví dụ "Name")
    // Với string? name = null là khi lời gọi hàm không cung cấp đối số này, nó sẽ nhận giá trị null trước khi [CallerMemberName] xử lý. Khi attribute áp dụng, null sẽ được thay thế bằng tên thành viên gọi
}