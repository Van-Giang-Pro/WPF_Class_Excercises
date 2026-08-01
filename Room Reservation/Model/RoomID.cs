using System;
using System.Collections.Generic;
using System.Text;

namespace Room_Reservation.Model
{
    public class RoomID
    {
        public int FloorNumber { get; }
        public int RoomNumber { get; }

        public RoomID(int floorNumber, int roomNumber)
        {
            FloorNumber = floorNumber;
            RoomNumber = roomNumber;
        }

        public override string ToString()
        {
            return $"{FloorNumber}{RoomNumber}"; 
        }

        public override bool Equals(object? obj) 
        {
            return obj is RoomID roomID &&
                FloorNumber == roomID.FloorNumber &&
                RoomNumber == roomID.RoomNumber;
        }
        // Nếu đối tượng là RoomID thì tạo biến roomID ép kiểu RoomID
        // Truyền vào đối tượng obj và đối tượng này có thể null

        public override int GetHashCode()
        {
            return HashCode.Combine(FloorNumber, RoomNumber);
        }
        // Trộn 2 số vào để cho ra 1 số nếu floor và room giống nhau thì sẽ cho ra cùng 1 số
        // Phòng A và Phòng B tuy cùng là 301 tầng 3 nhưng được tạo ở 2 chỗ khác nhau trong bộ nhớ, máy tính coi là 2 phòng khác nhau ❌ (sai về mặt logic)
        // Khi override, bạn bảo máy tính đừng nhìn bộ nhớ, hãy nhìn vào số tầng và số phòng thì A và B được coi là cùng một phòng
        // Nếu không có Hashcode thì nó không biết đến tủ nào để tìm, nó sẽ tìm 1000 cái tủ, rất là mất thời gian 
    }
}