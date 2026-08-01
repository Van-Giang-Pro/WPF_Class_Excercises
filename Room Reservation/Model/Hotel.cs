using System;
using System.Collections.Generic;
using System.Text;

namespace Room_Reservation.Model
{
    public class Hotel
    {
        private readonly ReservationBook _reservationBook;

        public string Name { get; }

        public Hotel(string name)
        {
            Name = name;
            _reservationBook = new ReservationBook();
        }
        /// <summary>
        ///  Get the reservation for a user
        /// </summary>
        /// <param name="username"></param>
        /// <returns>The reservation for the user</returns>
        public IEnumerable<Reservation> GetReservationForUser(string username)
        {
            return _reservationBook.GetReservationForUser(username);
        }
        /// <summary>
        /// Make a reservatiom
        /// </summary>
        /// <param name="reservation">The incoming reservation</param>
        /// <exception cref="ReservationConflictException"></exception>
        public void MakeReservation(Reservation reservation)
        {
            _reservationBook.AddReservation(reservation);
        }
    }
}
