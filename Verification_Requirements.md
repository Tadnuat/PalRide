# Yêu cầu Xác minh (Verification Requirements)

## Tổng quan
Hệ thống PalRide đã được cập nhật để yêu cầu xác minh tài liệu trước khi người dùng có thể thực hiện các hành động liên quan đến chuyến đi và tuyến đường.

## Điều kiện Xác minh

### 1. Tài xế (Driver) và Cả hai (Both)
**Yêu cầu:** Cả bằng lái xe và căn cước công dân phải được xác minh
- `DriverLicenseVerified = true`
- `CitizenIdVerified = true`

### 2. Hành khách (Passenger) và Cả hai (Both)
**Yêu cầu:** Chỉ cần căn cước công dân được xác minh
- `CitizenIdVerified = true`

## API bị ảnh hưởng

### Trip APIs (Yêu cầu xác minh)

#### Driver/Both APIs:
- `POST /api/trips` - Tạo chuyến đi
- `POST /api/trips/accept-request` - Chấp nhận yêu cầu hành khách
- `PUT /api/trips/{tripId}` - Cập nhật chuyến đi
- `PUT /api/trips/{tripId}/cancel` - Hủy chuyến đi
- `PUT /api/trips/{tripId}/complete` - Hoàn thành chuyến đi

#### Passenger/Both APIs:
- `POST /api/trips/request` - Tạo yêu cầu hành khách
- `PUT /api/trips/request/{tripId}/withdraw` - Rút yêu cầu hành khách

### Route APIs (Yêu cầu xác minh)

#### Tất cả người dùng:
- `POST /api/routes/register` - Đăng ký tuyến đường
- `PUT /api/routes/{routeId}` - Cập nhật tuyến đường
- `DELETE /api/routes/{routeId}` - Xóa tuyến đường

## Thông báo lỗi

Khi người dùng chưa được xác minh, API sẽ trả về:

### Driver/Both:
```
{
  "isSuccess": false,
  "message": "Driver license and citizen ID must be verified to perform this action"
}
```

### Passenger/Both:
```
{
  "isSuccess": false,
  "message": "Citizen ID must be verified to perform this action"
}
```

## Admin APIs để quản lý xác minh

### Cập nhật trạng thái xác minh bằng lái xe:
```
PUT /api/admin/users/{userId}/driver-license-verification
Body: true/false
```

### Cập nhật trạng thái xác minh căn cước:
```
PUT /api/admin/users/{userId}/citizen-id-verification
Body: true/false
```

### Cập nhật thông tin tài liệu:
```
PUT /api/admin/users/{userId}/documents
Body: {
  "driverLicenseNumber": "string",
  "driverLicenseImage": "string",
  "citizenId": "string",
  "citizenIdImage": "string"
}
```

## Cách hoạt động

1. **Kiểm tra tự động:** Mỗi khi người dùng gọi API liên quan đến chuyến đi hoặc tuyến đường, hệ thống sẽ tự động kiểm tra trạng thái xác minh của họ.

2. **Phân quyền theo vai trò:** Hệ thống kiểm tra vai trò của người dùng và áp dụng điều kiện xác minh tương ứng.

3. **Thông báo rõ ràng:** Nếu chưa đủ điều kiện, hệ thống sẽ trả về thông báo lỗi cụ thể để người dùng biết cần xác minh tài liệu nào.

## Lưu ý

- Các API tìm kiếm và xem thông tin (GET) không yêu cầu xác minh
- Chỉ các API tạo, cập nhật, xóa mới yêu cầu xác minh
- Admin có thể quản lý trạng thái xác minh của tất cả người dùng
