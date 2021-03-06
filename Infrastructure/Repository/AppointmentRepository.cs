using Dapper;
using Hospital.Application.Interfaces;
using Hospital.Core.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Infrastructure.Repository
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly IConfiguration configuration;
        public AppointmentRepository(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<int> DeleteAppointmentByIdAsync(string patientId)
        {
            var sql = "DELETE FROM Appointment WHERE PatientId = @PatientId";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(sql, new { PatientId = patientId });
                return result;
            }
        }

        public async Task<int> DeleteAsync(int id)
        {
            var sql = "DeleteAppointment";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(sql, new { Id = id }, commandType: CommandType.StoredProcedure);
                return result;
            }
        }

        public async Task<IReadOnlyList<Appointment>> GetAppointmentsByPatientIdAsync(string patientId)
        {
            var sql = "GetAppointmentsByPatientId";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.QueryAsync<Appointment>(sql, new { PatientId = patientId }, commandType: CommandType.StoredProcedure);
                return result.ToList();
            }
        }

        public async Task<IReadOnlyList<Appointment>> GetAllAsync(string sqlCommand = null)
        {
            var sql = "SELECT * FROM Appointment";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.QueryAsync<Appointment>(sql);
                return result.ToList();
            }
        }

        public async Task<Appointment> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM Appointment WHERE PatientId = @PatientId";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.QuerySingleOrDefaultAsync<Appointment>(sql, new { Id = id }, commandType: CommandType.StoredProcedure);
                return result;
            }
        }

        public async Task<int> UpsertAsync(Appointment entity)
        {
            entity.AddedOn = DateTime.Now;
            entity.ModifiedOn = DateTime.Now;
            var sql = "IF EXISTS " +
                "(SELECT * FROM Appointment WHERE Id = @Id) " +
                "UPDATE Appointment SET AppointmentDate = @AppointmentDate, IsEmpty = @IsEmpty, DoctorId = @DoctorId, " +
                "PatientId = @PatientId, ModifiedOn = @ModifiedOn  WHERE Id = @Id " +
                "ELSE " +
                "INSERT INTO Appointment (AppointmentDate, IsEmpty, DoctorId, PatientId, AddedOn) VALUES (@AppointmentDate, @IsEmpty, @DoctorId, @PatientId, @AddedOn) ";

            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.ExecuteAsync(sql, entity);
                return result;
            }
        }

        public async Task<int> GetAppointmentsSizeByDoctorId(int doctorId, string date)
        {
            var sql = $"Select count(*) from Appointment where doctorId=@doctorId and AppointmentDate Between '{date}' and GETDATE() ";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.QueryAsync<int>(sql, new { doctorId = doctorId });
                return result.First();
            }
        }

        public async Task<List<dynamic>> GetAppointmentsCountOrderByDate()
        {
            var sql = "Select Convert(date, AppointmentDate) AS Date, Count(Convert(date, AppointmentDate)) As AppointmentCount from Appointment Group By Convert(date, AppointmentDate) Order By AppointmentCount Desc";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                List<dynamic> result = (await connection.QueryAsync(sql)).ToList();

                return result;
            }

        }

        public async Task<dynamic> GetMinimumAppointmentDay()
        {
            var sql = "Select TOP 1 * from  ( Select Convert(date, AppointmentDate) AS Date, Count(Convert(date, AppointmentDate)) As AppointmentCount from Appointment Group By Convert(date, AppointmentDate))As EnDusukGun";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                dynamic result = await connection.QueryFirstAsync(sql);

                return result;
            }
        }

        public async Task<dynamic> GetMaximumAppointmentDay()
        {
            var sql = "Select TOP 1 * from  ( Select Convert(date, AppointmentDate) AS Date, Count(Convert(date, AppointmentDate)) As AppointmentCount from Appointment Group By Convert(date, AppointmentDate))As EnYuksekGun Order By AppointmentCount DESC";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var result = await connection.QueryFirstAsync(sql);

                return result;
            }
        }

        public async Task<List<dynamic>> GetAppointmentsDetailByDateRange(string patientId, string startingTime)
        {
            var sql = "GetAppointmentsDetailByDateRange ";

            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var values = new { PatientId = patientId, StartingTime = startingTime };
                List<dynamic> result = (await connection.QueryAsync(sql, values, commandType: CommandType.StoredProcedure)).ToList();

                return result;
            }

            //var results = connection.Query(procedure, values, commandType: CommandType.StoredProcedure).ToList();
        }

        public async Task<List<dynamic>> GetAppointmentCountByHours(int startHour, int endHour)
        {
            var sql = "FindAppointmentCountWithHour";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var values = new { StartTime = startHour, EndTime = endHour };
                List<dynamic> result = (await connection.QueryAsync(sql, values, commandType: CommandType.StoredProcedure)).ToList();

                return result;
            }
        }

        public async Task<List<dynamic>> GetWeekDayByAppointmentCount()
        {
            var sql = "FindWeekDayWithAppointmentCount";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                List<dynamic> result = (await connection.QueryAsync(sql, commandType: CommandType.StoredProcedure)).ToList();

                return result;
            }
        }

        public async Task<bool> IsAvailable(string appointmentDate, int doctorId)
        {
            var sql = "SELECT * FROM Appointment WHERE AppointmentDate = @appointmentDate AND DoctorId = @doctorId";
            using (var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var values = new { AppointmentDate = appointmentDate, DoctorId = doctorId };
                var result = await connection.QueryAsync<Appointment>(sql, values);

                var numberOfResults = result.ToList().Count();

                if (numberOfResults == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
