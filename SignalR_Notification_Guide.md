# Hệ thống thông báo SignalR - PalRide

## 🚀 Tổng quan

Hệ thống thông báo SignalR cho phép gửi thông báo real-time đến người dùng dựa trên:
- **User ID** - Gửi đến user cụ thể
- **Role** - Gửi đến tất cả user có role (Driver, Passenger, Both)
- **Trip ID** - Gửi đến tất cả user liên quan đến chuyến đi
- **Broadcast** - Gửi đến tất cả user

## 📱 Các loại thông báo

### 🔴 Thông báo "Quan trọng" (Important)

1. **"Chuyến đi đã hoàn tất"**
   - **Trigger:** `PUT /api/trips/{tripId}/complete`
   - **Gửi đến:** Tất cả passenger trong chuyến đi
   - **Message:** "Chuyến đi của bạn đã hoàn tất, vui lòng đánh giá hành khách."

2. **"Tài xế vừa hủy chuyến đi"**
   - **Trigger:** `PUT /api/trips/{tripId}/cancel`
   - **Gửi đến:** Tất cả passenger đã booking
   - **Message:** "Tài xế đã hủy chuyến đi vào ngày {date}."

3. **"Hành khách hủy booking"**
   - **Trigger:** `PUT /api/bookings/{bookingId}/cancel`
   - **Gửi đến:** Driver
   - **Message:** "Hành khách đã hủy booking chuyến đi của bạn."

4. **"Tài xế chấp nhận booking"**
   - **Trigger:** `PUT /api/bookings/{bookingId}/accept`
   - **Gửi đến:** Passenger
   - **Message:** "Tài xế đã chấp nhận booking của bạn."

5. **"Tài xế đã nhận yêu cầu"**
   - **Trigger:** `POST /api/trips/accept-request`
   - **Gửi đến:** Passenger
   - **Message:** "Tài xế đã nhận yêu cầu chuyến đi của bạn."

6. **"Đang tìm nửa kia cho chuyến đi"**
   - **Trigger:** `POST /api/trips/request`
   - **Gửi đến:** Driver phù hợp (có điều kiện)
   - **Message:** "Có yêu cầu chuyến đi mới từ {pickup} đến {dropoff} vào {time}."
   - **Điều kiện:** 
     - Role = Driver hoặc Both
     - Không có trip trùng thời gian (trong vòng 2 giờ)
     - Có route phù hợp (nếu có lưu route)

### 🔵 Thông báo "Khác" (Other)

7. **Thông báo Admin tự tạo**
   - **API:** `POST /api/admin/notifications/create`
   - **Có thể chọn:** Role (Driver, Passenger, Both) hoặc User cụ thể
   - **Type:** "Other"

8. **Thông báo khi tạo Voucher**
   - **Trigger:** `POST /api/vouchers` (với field Message)
   - **Gửi đến:** Theo role được chọn trong voucher
   - **Message:** Lời nhắn từ admin trong field Message

## 🔧 Cách sử dụng

### 📋 Phân quyền API

**Admin APIs:**
- `POST /api/admin/notifications/create` - Tạo thông báo (theo role hoặc user cụ thể)
- `POST /api/admin/notifications/create-bulk` - Tạo thông báo cho nhiều user
- `GET /api/admin/notifications/user/{userId}` - Xem thông báo của user cụ thể

**User APIs:**
- `GET /api/notifications/my` - Xem thông báo của mình
- `PUT /api/notifications/{notificationId}/read` - Đánh dấu đã đọc
- `PUT /api/notifications/read-all` - Đánh dấu tất cả đã đọc
- `GET /api/notifications/unread-count` - Đếm thông báo chưa đọc

### Admin tạo thông báo

#### 1. Gửi theo Role (tất cả user có role đó)
```json
POST /api/admin/notifications/create
{
  "title": "Thông báo cho tài xế",
  "message": "Có cập nhật mới về quy định",
  "type": "Other",
  "targetRole": "Driver"
}
```

#### 2. Gửi đến user cụ thể
```json
POST /api/admin/notifications/create
{
  "title": "Thông báo cá nhân",
  "message": "Tài khoản của bạn có vấn đề",
  "type": "Important",
  "targetUserId": 123
}
```

#### 3. Gửi đến nhiều user cụ thể
```json
POST /api/admin/notifications/create-bulk
{
  "title": "Thông báo VIP",
  "message": "Chúc mừng bạn là user VIP",
  "type": "Other",
  "targetUserIds": [1, 5, 10, 15, 20]
}
```

#### 4. Gửi đến tất cả user

**Thông báo chung (không liên quan entity):**
```json
POST /api/admin/notifications/create
{
  "title": "Thông báo hệ thống",
  "message": "Hệ thống sẽ bảo trì vào 2h sáng",
  "type": "Other"
  // relatedEntityType và relatedEntityId = null (không cần)
}
```

**Thông báo về entity cụ thể:**
```json
POST /api/admin/notifications/create
{
  "title": "Chuyến đi mới",
  "message": "Có chuyến đi mới từ A đến B",
  "type": "Important",
  "relatedEntityType": "Trip",
  "relatedEntityId": 14
}
```

### Admin xem thông báo của user

```json
GET /api/admin/notifications/user/{userId}
```

### Admin tạo voucher với thông báo

```json
POST /api/vouchers
{
  "code": "SUMMER2024",
  "description": "Giảm giá mùa hè",
  "discountValue": 20,
  "toPassengers": true,
  "toDrivers": false,
  "message": "Chúc mừng! Bạn nhận được voucher giảm 20% cho chuyến đi tiếp theo."
}
```

### User kết nối SignalR (Frontend)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub", {
        accessTokenFactory: () => localStorage.getItem("token")
    })
    .build();

connection.start().then(() => {
    console.log("Connected to SignalR");
});

connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);
    // Hiển thị thông báo trong UI
    showNotification(notification);
});

function showNotification(notification) {
    // Tạo toast notification
    const toast = document.createElement('div');
    toast.className = `notification-toast ${notification.type.toLowerCase()}`;
    toast.innerHTML = `
        <div class="notification-header">
            <h4>${notification.title}</h4>
            <span class="timestamp">${new Date(notification.timestamp).toLocaleTimeString()}</span>
        </div>
        <p>${notification.message}</p>
    `;
    
    document.body.appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        toast.remove();
    }, 5000);
}
```

### User quản lý thông báo của mình

```javascript
// Lấy danh sách thông báo của user hiện tại
GET /api/notifications/my

// Đánh dấu đã đọc
PUT /api/notifications/{notificationId}/read

// Đánh dấu tất cả đã đọc
PUT /api/notifications/read-all

// Lấy số thông báo chưa đọc
GET /api/notifications/unread-count
```

## 🗄️ Cấu trúc Database

### Bảng Notification

```sql
CREATE TABLE [dbo].[Notification] (
    [NotificationId] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] INT NULL,                    -- NULL = broadcast
    [UserRole] NVARCHAR(50) NULL,         -- Driver, Passenger, Both
    [Title] NVARCHAR(255) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,         -- Important, Other
    [IsRead] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] INT NULL,                 -- Admin ID
    [RelatedEntityType] NVARCHAR(50) NULL, -- Trip, Booking, Voucher
    [RelatedEntityId] INT NULL,           -- ID của entity liên quan
    
    FOREIGN KEY ([UserId]) REFERENCES [User]([UserId]),
    FOREIGN KEY ([CreatedBy]) REFERENCES [Admin]([AdminId])
);
```

## 🎯 Tính năng

### 🔄 **Dual Notification System**
Hệ thống thông báo hoạt động theo cơ chế **kép**:
- **SignalR Real-time**: Gửi thông báo ngay lập tức đến user đang online
- **Database Persistence**: Lưu thông báo vào database để user có thể xem lại

**Lợi ích:**
- ✅ User online nhận thông báo ngay lập tức
- ✅ User offline có thể xem lại thông báo khi vào app
- ✅ Không bị mất thông báo khi user không online
- ✅ Có thể đếm số thông báo chưa đọc

- ✅ **Real-time notifications** qua SignalR
- ✅ **Role-based targeting** (Driver, Passenger, Both)
- ✅ **User-specific targeting**
- ✅ **Trip-based notifications**
- ✅ **Admin notification management**
- ✅ **Voucher notifications**
- ✅ **Automatic notifications** cho các action quan trọng
- ✅ **Notification persistence** trong database
- ✅ **Unread count tracking**
- ✅ **Dual notification system** - Vừa gửi SignalR vừa lưu database

## 🔧 Cài đặt

1. **Cập nhật Database:**
   
   **Nếu bảng Notification đã tồn tại:**
   ```sql
   -- Chạy script Database_Update_Notification.sql
   ```
   
   **Nếu tạo mới hoàn toàn:**
   ```sql
   -- Chạy script Create_Notification_Table.sql
   ```

2. **Cấu hình SignalR:**
   ```csharp
   // Trong Program.cs đã được cấu hình
   builder.Services.AddSignalR();
   app.MapHub<NotificationHub>("/notificationHub");
   ```

3. **Frontend kết nối:**
   ```javascript
   // Sử dụng SignalR client library
   npm install @microsoft/signalr
   ```

## 📝 Lưu ý

- Thông báo được lưu trong database để user có thể xem lại
- SignalR chỉ gửi thông báo real-time, không thay thế database
- User cần đăng nhập để nhận thông báo
- Thông báo được phân loại theo Type (Important/Other) để UI hiển thị khác nhau

