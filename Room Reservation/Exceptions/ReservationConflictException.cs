namespace Room_Reservation.Exceptions;

public class ReservationConflictException : Exception           
{
    public Reservation ExistingReservation { get; }
    public Reservation IncomingReservation { get; }
    
    public ReservationConflictException(Reservation existingReservation, Reservation incomingReservation)
    {
        ExistingReservation = existingReservation;
        IncomingReservation = incomingReservation;
    }

    public ReservationConflictException(string? message, Reservation existingReservation, Reservation incomingReservation) : base(message)
    {
        ExistingReservation = existingReservation;
        IncomingReservation = incomingReservation;
    }

    public ReservationConflictException(string? message, Exception? innerException, Reservation existingReservation, Reservation incomingReservation) : base(message, innerException)
    {
        ExistingReservation = existingReservation;
        IncomingReservation = incomingReservation;
    }
}