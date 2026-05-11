using System;
using System.Collections.Generic;
using System.Linq;
using NotificationSystem.Models;
using NotificationSystem.Contracts;
using Microsoft.Extensions.Configuration;
using System.Data;
using Npgsql;
using System.IO;
using NotificationSystem.Exceptions;


namespace NotificationSystem.Data
{
    public class NotificationRepository : INotificationRepository
    {        
        private readonly string _connectionString = string.Empty;
        public NotificationRepository()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            _connectionString = config["ConnectionStrings:GensparkDb"] ?? "";
        }

        public User? GetUserById(string userId){
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                string query = "SELECT * FROM users WHERE id = @id";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@id", Guid.Parse(userId));
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new User(
                        row["id"].ToString()!,
                        row["username"].ToString()!,
                        row["email"].ToString()!,
                        row["phone_number"]?.ToString() ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
            return null;
        }

        public User? GetUserByPhone(string phone)
        {       
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                string query = "SELECT * FROM users WHERE phone_number = @phone";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@phone", phone);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new User(
                        row["id"].ToString()!,
                        row["username"].ToString()!,
                        row["email"].ToString()!,
                        row["phone_number"]?.ToString() ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
            return null;
        }

        public List<Notification> GetNotifications(string userId)
        {
            List<Notification> list = new List<Notification>();
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                string query = @"SELECT n.*, u_s.username as s_name, u_s.email as s_email, u_s.phone_number as s_phone, 
                                       u_r.username as r_name, u_r.email as r_email, u_r.phone_number as r_phone 
                                FROM notifications n 
                                LEFT JOIN users u_s ON n.sender_id = u_s.id 
                                JOIN users u_r ON n.receiver_id = u_r.id 
                                WHERE (n.receiver_id = @id AND n.is_deleted_by_receiver = false) 
                                   OR (n.sender_id = @id AND n.is_deleted_by_sender = false)";
                
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@id", Guid.Parse(userId));
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    User? sender = row["sender_id"] != DBNull.Value ? 
                        new User(row["sender_id"].ToString()!, row["s_name"].ToString()!, row["s_email"].ToString()!, row["s_phone"]?.ToString() ?? "") : null;
                    User receiver = new User(row["receiver_id"].ToString()!, row["r_name"].ToString()!, row["r_email"].ToString()!, row["r_phone"]?.ToString() ?? "");
                    
                    string displayStatus = row["receiver_id"].ToString() == userId ? "Received" : "Sent";

                    Notification notification = row["type"].ToString() == "Email" ?
                        new EmailNotification(row["id"].ToString()!, row["message"].ToString()!, (DateTime)row["sent_at"], receiver, sender!, displayStatus) :
                        new SmsNotification(row["id"].ToString()!, row["message"].ToString()!, (DateTime)row["sent_at"], receiver, sender!, displayStatus);
                    
                    list.Add(notification);
                }
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
            return list;
        }

        public List<Notification> this[string userId]
        {
            get { return GetNotifications(userId); }
        }

        public List<Notification> FilterNotifications(string userId, string notificationStatus)
        {
            List<Notification> list = new List<Notification>();
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            
            // Map the string status back to DB integer (0 for Sent, 1 for Received)
            int statusInt = notificationStatus == "Received" ? 1 : 0;

            try
            {
                string whereClause = notificationStatus == "Received" ? 
                    "n.receiver_id = @id AND n.is_deleted_by_receiver = false" : 
                    "n.sender_id = @id AND n.is_deleted_by_sender = false";
                string query = $@"SELECT n.*, u_s.username as s_name, u_s.email as s_email, u_s.phone_number as s_phone, 
                                       u_r.username as r_name, u_r.email as r_email, u_r.phone_number as r_phone 
                                FROM notifications n 
                                LEFT JOIN users u_s ON n.sender_id = u_s.id 
                                JOIN users u_r ON n.receiver_id = u_r.id 
                                WHERE {whereClause}";
                
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@id", Guid.Parse(userId));
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    User? sender = row["sender_id"] != DBNull.Value ? 
                        new User(row["sender_id"].ToString()!, row["s_name"].ToString()!, row["s_email"].ToString()!, row["s_phone"]?.ToString() ?? "") : null;
                    User receiver = new User(row["receiver_id"].ToString()!, row["r_name"].ToString()!, row["r_email"].ToString()!, row["r_phone"]?.ToString() ?? "");
                    
                    string displayStatus = row["receiver_id"].ToString() == userId ? "Received" : "Sent";

                    Notification notification = row["type"].ToString() == "Email" ?
                        new EmailNotification(row["id"].ToString()!, row["message"].ToString()!, (DateTime)row["sent_at"], receiver, sender!, displayStatus) :
                        new SmsNotification(row["id"].ToString()!, row["message"].ToString()!, (DateTime)row["sent_at"], receiver, sender!, displayStatus);
                    
                    list.Add(notification);
                }
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
            return list;
        }

        public string CheckUserStatus(string userId)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                string query = "SELECT COUNT(*) FROM users WHERE id = @id";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@id", Guid.Parse(userId));
                DataTable dt = new DataTable();
                da.Fill(dt);

                return (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0) ? "Exists" : "New";
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }

        public void AddNewUser(User user)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            NpgsqlCommand? cmd = null;
            try
            {
                conn.Open();
                string query = "INSERT INTO users (id, username, email, phone_number) VALUES (@id, @name, @email, @phone)";
                cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(user.Id));
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@phone", user.PhoneNumber);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                cmd?.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }

        public void UpdateUser(User user)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            NpgsqlCommand? cmd = null;
            try
            {
                conn.Open();
                string query = "UPDATE users SET username=@name, email=@email, phone_number=@phone WHERE id=@id";
                cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(user.Id));
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@phone", user.PhoneNumber);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                cmd?.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }

        public string StoreNotification(string userId, Notification notification)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            NpgsqlCommand? cmd = null;
            try
            {
                conn.Open();
                string query = "INSERT INTO notifications (id, message, type, status, sender_id, receiver_id) VALUES (@id, @msg, @type, @status, @sender, @receiver)";
                cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(notification.Id));
                cmd.Parameters.AddWithValue("@msg", notification.Message);
                cmd.Parameters.AddWithValue("@type", notification is EmailNotification ? "Email" : "SMS");
                cmd.Parameters.AddWithValue("@status", notification.Status == "Received" ? 1 : 0);
                cmd.Parameters.AddWithValue("@sender", notification.Sender != null ? (object)Guid.Parse(notification.Sender.Id) : DBNull.Value);
                cmd.Parameters.AddWithValue("@receiver", Guid.Parse(notification.Receiver.Id));
                cmd.ExecuteNonQuery();
                return "Stored";
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                cmd?.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }

        public Notification? GetNotificationById(string id, string userId)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            try
            {
                string query = "SELECT * FROM notifications WHERE id = @id";
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(query, conn);
                da.SelectCommand!.Parameters.AddWithValue("@id", Guid.Parse(id));
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    return null; 
                }
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
            return null;
        }

        public void DeleteNotification(string id, string userId)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            NpgsqlCommand? cmd = null;
            try
            {
                conn.Open();
                // Update the flag based on whether the user is the sender or receiver
                string query = @"UPDATE notifications 
                                SET is_deleted_by_sender = CASE WHEN sender_id = @u_id THEN true ELSE is_deleted_by_sender END,
                                    is_deleted_by_receiver = CASE WHEN receiver_id = @u_id THEN true ELSE is_deleted_by_receiver END
                                WHERE id = @n_id;
                                
                                -- Physically delete only if both parties have 'deleted' it
                                DELETE FROM notifications WHERE is_deleted_by_sender = true AND is_deleted_by_receiver = true;";
                
                cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@n_id", Guid.Parse(id));
                cmd.Parameters.AddWithValue("@u_id", Guid.Parse(userId));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                cmd?.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }

        public void UpdateNotification(string id, string userId, string newMessage)
        {
            NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
            NpgsqlCommand? cmd = null;
            try
            {
                conn.Open();
                string query = "UPDATE notifications SET message = @msg WHERE id = @id";
                cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(id));
                cmd.Parameters.AddWithValue("@msg", newMessage);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new NotificationException("Database Error: " + ex.Message);
            }
            finally
            {
                cmd?.Dispose();
                if (conn.State != ConnectionState.Closed) conn.Close();
                conn.Dispose();
            }
        }
    }
}