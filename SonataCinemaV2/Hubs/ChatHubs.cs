using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using SonataCinemaV2.Helper;
using SonataCinemaV2.Models;

namespace SonataCinemaV2.Hubs
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> UserConnections = new Dictionary<string, string>();
        private static Dictionary<string, string> StaffConnections = new Dictionary<string, string>();
        private const string StaffGroup = "Staff";
        private CinemaV3Entities db = new CinemaV3Entities();

        public override Task OnConnected()
        {
            Debug.WriteLine($"Client connected: {Context.ConnectionId}");
            return base.OnConnected();
        }

        public async Task JoinStaffGroup()
        {
            try
            {
                var context = Context.Request.GetHttpContext();
                var userEmail = context.User.Identity.Name;

                var nhanVien = db.NhanViens.FirstOrDefault(n => n.Email == userEmail);
                if (nhanVien != null && (nhanVien.QuyenHan == "Admin" || nhanVien.QuyenHan == "Staff"))
                {
                    await Groups.Add(Context.ConnectionId, StaffGroup);
                    StaffConnections[userEmail] = Context.ConnectionId; // Lưu connection của staff

                    Debug.WriteLine($"Staff joined: {Context.ConnectionId} - {userEmail}");
                    Debug.WriteLine($"Current StaffConnections: {string.Join(", ", StaffConnections)}");
                }
                else
                {
                    Debug.WriteLine($"Unauthorized access attempt: {userEmail}");
                    throw new HubException("Unauthorized access to staff group");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in JoinStaffGroup: {ex.Message}");
                throw;
            }
        }


        public async Task RequestStaffChat(string userId, string userName)
        {
            try
            {
                Debug.WriteLine($"✅ RequestStaffChat called - UserId: {userId}, UserName: {userName}");
                Console.WriteLine($"✅ RequestStaffChat called - UserId: {userId}, UserName: {userName}");

                UserConnections[userId] = Context.ConnectionId;

                var request = new
                {
                    userId = userId,
                    userName = userName,
                    timestamp = DateTime.Now
                };

                Debug.WriteLine($"🚀 Sending chat request to Staff group");
                Console.WriteLine($"🚀 Sending chat request to Staff group");

                await Clients.Group(StaffGroup).NewChatRequest(request);

                Console.WriteLine($"📩 Server gửi yêu cầu chat: {request?.userId}, {request?.userName}");
                Debug.WriteLine($"📩 Server gửi yêu cầu chat: {request?.userId}, {request?.userName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error in RequestStaffChat: {ex.Message}");
                Console.WriteLine($"❌ Error in RequestStaffChat: {ex.Message}");
                throw new HubException($"Lỗi khi yêu cầu chat: {ex.Message}");
            }
        }


        private string FormatRoomId(string userId, string staffId)
        {
            // Lấy username từ email
            string userUsername = userId.Split('@')[0].ToLower();
            string staffUsername = staffId.Split('@')[0].ToLower();
            return $"chat_{userUsername}_{staffUsername}";
        }

        public async Task AcceptChat(string staffId, string staffName, string userId)
        {
            try
            {
                Debug.WriteLine($"AcceptChat called - StaffId: {staffId}, UserId: {userId}");

                // Sử dụng FormatRoomId để tạo roomId
                string roomId = FormatRoomId(userId, staffId);
                Debug.WriteLine($"Creating room: {roomId}");

                if (UserConnections.ContainsKey(userId))
                {
                    await Groups.Add(UserConnections[userId], roomId);
                    await Groups.Add(Context.ConnectionId, roomId);

                    await Clients.Client(UserConnections[userId]).chatAccepted(staffId);
                    await Clients.Group(StaffGroup).chatTaken(userId);

                    Debug.WriteLine($"Chat accepted - Room: {roomId}, Staff: {staffId}");
                    Debug.WriteLine($"User connection: {UserConnections[userId]}");
                    Debug.WriteLine($"Staff connection: {Context.ConnectionId}");
                }
                else
                {
                    throw new HubException("User không tồn tại");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AcceptChat: {ex.Message}");
                throw;
            }
        }

        public async Task SendMessage(string roomId, string senderId, string message, string role)
        {
            try
            {
                Debug.WriteLine($"SendMessage - Room: {roomId}, Sender: {senderId}, Role: {role}");
                await Clients.Group(roomId).receiveMessage(senderId, message, role, DateTime.Now);
                Debug.WriteLine($"Message sent to room {roomId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
        }

        public async Task EndChat(string roomId, string userId, string reason = null)
        {
            try
            {
                Debug.WriteLine($"EndChat called - Room: {roomId}, User: {userId}");

                // Thông báo cho tất cả người trong room
                await Clients.Group(roomId).chatEnded(userId, reason);

                // Xóa các connections khỏi room
                if (UserConnections.ContainsKey(userId))
                {
                    await Groups.Remove(UserConnections[userId], roomId);
                }
                await Groups.Remove(Context.ConnectionId, roomId);

                // Nếu là staff, cập nhật UI dashboard
                if (StaffConnections.ContainsValue(Context.ConnectionId))
                {
                    await Clients.Group(StaffGroup).activeChatEnded(roomId);
                }

                Debug.WriteLine($"Chat ended successfully - Room: {roomId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ending chat: {ex.Message}");
                throw;
            }
        }
    }
}