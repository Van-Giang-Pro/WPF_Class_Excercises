using System.Data;
using Xunit;
using Calculator;

public class MathHelperTests
{
    private readonly MathHelper _math = new MathHelper();
    // Tạo một đối tợng MathHelper để dùng trong các test như là 1 cái máy tính để cộng số
    // Ta có readonly là tạo một lần, không gán lại được
    [Theory] // Test này chạy nhều lần với nhiều bộ số khác nhau
    [InlineData(2, 3, 5)]
    [InlineData(-1, 1, 0)]
    public void Add_Numbers_ShouldReturnCorrectResult(int a, int b, int excepted)
    {
        Assert.Equal(excepted, _math.Add(a, b)); 
        // Khẳng định kết quả mong đợi phải bằng kết quả máy tính cộng ra
    }
    [Fact] // Test đơn giản chạy đúng một lần
    public void IsEven_ShouldReturnTrue() => Assert.True(_math.IsEven(4));
    // Khẳng định số 4 là số chẵn phải trả về true
    [Fact]
    public void IsOdd_ShoulReturnFalse() => Assert.False(_math.IsEven(7));
}