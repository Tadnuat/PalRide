# Hướng dẫn Validation và Error Handling cho Update Profile

## Các Validation đã thêm

### 1. ✅ Validation Trường Bắt Buộc

#### Full Name
- **Kiểm tra**: Không được null hoặc empty
- **Lỗi**: `"Full name is required and cannot be empty"`
- **Giới hạn**: Tối đa 100 ký tự
- **Lỗi**: `"Full name cannot exceed 100 characters"`

#### Role
- **Kiểm tra**: Không được null hoặc empty
- **Lỗi**: `"Role is required and cannot be empty"`
- **Giá trị hợp lệ**: `"Passenger"`, `"Driver"`, `"Both"`
- **Lỗi**: `"Role must be one of: Passenger, Driver, Both"`

### 2. ✅ Validation Trường Tùy Chọn

#### Gender
- **Giá trị hợp lệ**: `"Male"`, `"Female"`, `"Other"`
- **Lỗi**: `"Gender must be one of: Male, Female, Other"`

#### Date of Birth
- **Kiểm tra**: Không được trong tương lai
- **Lỗi**: `"Date of birth cannot be in the future"`
- **Kiểm tra**: Tuổi tối thiểu 16
- **Lỗi**: `"User must be at least 16 years old"`

#### Phone Number
- **Format**: Số điện thoại Việt Nam hợp lệ
- **Pattern**: `^(\+84|84|0)[1-9][0-9]{8,9}$`
- **Lỗi**: `"Phone number must be a valid Vietnamese phone number (10-11 digits starting with 0 or +84)"`
- **Kiểm tra trùng lặp**: `"Phone number is already taken by another user"`

### 3. ✅ Validation Độ Dài Chuỗi

| Trường | Giới hạn | Lỗi |
|--------|----------|-----|
| Full Name | 100 ký tự | `"Full name cannot exceed 100 characters"` |
| Introduce | 500 ký tự | `"Introduce cannot exceed 500 characters"` |
| University | 150 ký tự | `"University name cannot exceed 150 characters"` |
| Student ID | 50 ký tự | `"Student ID cannot exceed 50 characters"` |
| Driver License Number | 50 ký tự | `"Driver license number cannot exceed 50 characters"` |
| Citizen ID | 50 ký tự | `"Citizen ID cannot exceed 50 characters"` |

### 4. ✅ Validation Database

#### User Existence
- **Kiểm tra**: User có tồn tại không
- **Lỗi**: `"User not found. Please check your authentication token"`

#### Phone Number Uniqueness
- **Kiểm tra**: Số điện thoại có bị trùng không
- **Lỗi**: `"Phone number is already taken by another user"`

## Error Handling Chi Tiết

### 1. ✅ Specific Exception Handling

#### KeyNotFoundException
```json
{
  "isSuccess": false,
  "message": "User not found. Please check your authentication token and try again."
}
```

#### InvalidOperationException
```json
{
  "isSuccess": false,
  "message": "Phone number is already taken by another user"
}
```

#### ArgumentException
```json
{
  "isSuccess": false,
  "message": "Invalid input data: [chi tiết lỗi]"
}
```

#### SqlException
```json
{
  "isSuccess": false,
  "message": "Database error occurred. Please try again later."
}
```

#### General Exception
```json
{
  "isSuccess": false,
  "message": "An unexpected error occurred while updating your profile. Please try again later."
}
```

### 2. ✅ Data Sanitization

- **Trim whitespace**: Tất cả string fields được trim
- **Null handling**: Empty strings được chuyển thành null
- **Date formatting**: DateOfBirth được chuyển thành DateOnly

## Ví dụ Response Lỗi

### 1. Validation Lỗi
```json
{
  "isSuccess": false,
  "message": "Full name is required and cannot be empty"
}
```

### 2. Role Lỗi
```json
{
  "isSuccess": false,
  "message": "Role must be one of: Passenger, Driver, Both"
}
```

### 3. Phone Number Lỗi
```json
{
  "isSuccess": false,
  "message": "Phone number must be a valid Vietnamese phone number (10-11 digits starting with 0 or +84)"
}
```

### 4. Date of Birth Lỗi
```json
{
  "isSuccess": false,
  "message": "User must be at least 16 years old"
}
```

### 5. Database Lỗi
```json
{
  "isSuccess": false,
  "message": "Phone number is already taken by another user"
}
```

## Response Thành Công

```json
{
  "isSuccess": true,
  "message": "Profile updated successfully",
  "result": {
    "userId": 1,
    "fullName": "Nguyễn Văn A",
    "email": "user@example.com",
    "phoneNumber": "0901234567",
    "role": "Both",
    "isActive": true,
    "introduce": "Tôi là sinh viên",
    "university": "FPT University",
    "studentId": "STU001",
    "dateOfBirth": "2000-01-01T00:00:00Z",
    "gender": "Male",
    "avatar": "https://example.com/avatar.jpg",
    "driverLicenseNumber": "123456789",
    "driverLicenseImage": "https://example.com/license.jpg",
    "citizenId": "123456789012",
    "citizenIdImage": "https://example.com/citizen.jpg",
    "driverLicenseVerified": false,
    "citizenIdVerified": false
  }
}
```

## Lợi ích

1. **Validation toàn diện**: Kiểm tra tất cả trường input
2. **Error messages rõ ràng**: Thông báo lỗi cụ thể và dễ hiểu
3. **Data sanitization**: Làm sạch dữ liệu trước khi lưu
4. **Exception handling**: Xử lý lỗi theo từng loại cụ thể
5. **User-friendly**: Thông báo lỗi thân thiện với người dùng
6. **Debug support**: Log lỗi chi tiết cho developer
7. **Security**: Validation input để tránh injection attacks

## Cách Test

### 1. Test Validation
```bash
# Test empty full name
curl -X PUT /api/auth/profile \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "", "role": "Passenger"}'
```

### 2. Test Invalid Role
```bash
curl -X PUT /api/auth/profile \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Test User", "role": "InvalidRole"}'
```

### 3. Test Invalid Phone
```bash
curl -X PUT /api/auth/profile \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Test User", "role": "Passenger", "phoneNumber": "123"}'
```

### 4. Test Future Date
```bash
curl -X PUT /api/auth/profile \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"fullName": "Test User", "role": "Passenger", "dateOfBirth": "2030-01-01T00:00:00Z"}'
```

