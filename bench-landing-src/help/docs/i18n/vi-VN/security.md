# Bảo mật

Bảo mật kiểm tra Microsoft Defender, tường lửa, chứng chỉ và các dịch vụ đăng nhập. Mục này cũng cho thấy trạng thái TPM và bảo mật nền tảng mà BitLocker và Windows Hello phụ thuộc vào.

Hướng dẫn từng bước: [Mã hóa ổ đĩa bằng BitLocker](encrypt-with-bitlocker.html) và [Tăng quyền riêng tư cho Windows](make-windows-private.html).

## Công cụ trong mục này

Kiểm tra bảo vệ, tường lửa, chứng chỉ và dịch vụ đăng nhập.

### Microsoft Defender

Kiểm tra tình trạng bảo vệ và chữ ký của Defender. Tự động sửa chỉ làm mới chữ ký khi Defender báo cáo bảo vệ đang hoạt động và chữ ký đã lỗi thời. Dùng các nguồn chữ ký đã cấu hình. Không thay đổi bảo vệ, loại trừ hay chính sách; không quét phần mềm độc hại.

### Trung tâm bảo mật Windows

Xem lại có giới hạn các đăng ký phần mềm diệt vi-rút kèm trạng thái thô của nhà cung cấp, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của Trung tâm bảo mật Windows mà không diễn giải tình trạng bảo vệ. Danh sách trống không phải là kết luận về tình trạng bảo vệ, và số lượng đăng ký, trạng thái thô hay lỗi trong quá khứ không thể cho phép khôi phục hoặc thay đổi bảo mật. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Việc giám sát trạng thái bảo mật và thông báo hiện có có thể tiếp tục. Cấu hình phần mềm diệt vi-rút, đăng ký nhà cung cấp, cài đặt bảo vệ, loại trừ, thông báo, chính sách và kiểu khởi động được giữ nguyên. Không có yêu cầu quét, cập nhật hay thay đổi bảo vệ nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm sự hiện diện của phần mềm diệt vi-rút, trạng thái bảo vệ, độ mới của chữ ký, việc gửi thông báo, quyền sở hữu đăng ký hay chính sách hiệu lực.

### Dịch vụ tường lửa

Đọc cài đặt bật hồ sơ Miền, Riêng tư và Công cộng từ ActiveStore, sau đó kiểm tra các dịch vụ Base Filtering Engine và Tường lửa Windows Defender. Khôi phục tùy chọn chỉ khởi động các dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Chính sách lọc hiện có có thể có hiệu lực và làm gián đoạn truy cập mạng hoặc truy cập từ xa. Kiểu khởi động, quyền dịch vụ, quy tắc tường lửa, hồ sơ và cài đặt IPsec được giữ nguyên. Không đặt lại, không thay đổi quy tắc và không dừng dịch vụ. Việc xác minh chỉ xác lập trạng thái đang chạy của dịch vụ; khả năng bảo vệ thực tế và kết nối vẫn chưa được xác minh.

### Hạn dùng chứng chỉ

Kiểm tra ngày của chứng chỉ và dựng chuỗi tin cậy cục bộ chỉ dùng bộ nhớ đệm cho kho cá nhân của máy và của người dùng hiện tại, sau đó trả về một kết luận hoạt động có giới hạn. Thu hồi, việc máy chủ xuất trình, liên kết, đăng ký, quyền sở hữu chính sách, tin cậy từ xa và khả năng dùng khóa riêng vẫn chưa được xác minh. Không có chứng chỉ, cài đặt tin cậy, đăng ký, liên kết, khóa riêng, ứng dụng, dịch vụ hay chính sách nào bị thay đổi.

### Trình quản lý thông tin xác thực

Kiểm tra tính khả dụng của dịch vụ Trình quản lý thông tin xác thực. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các ứng dụng hiện có có thể tiếp tục yêu cầu thông tin xác thực đã lưu. Nội dung kho, mật khẩu, khóa truy cập, chứng chỉ, mã thông báo, đăng ký thông tin xác thực, quyền, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có nội dung hay thao tác thông tin xác thực nào được yêu cầu. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc truy xuất thông tin xác thực, xác thực hay truy cập của ứng dụng.

### Trình quản lý tài khoản web

Xem lại lịch sử bộ môi giới tài khoản đã được làm sạch nội dung và tính khả dụng chính xác của Trình quản lý tài khoản web. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các ứng dụng hiện có có thể tiếp tục yêu cầu tài khoản và mã thông báo theo chính sách hiện tại. Sự kiện trong quá khứ không thể cho phép khôi phục hay xác lập trạng thái tài khoản hiện tại. Tài khoản, thông tin xác thực, mã thông báo, cookie, đăng ký ứng dụng, quyền, trạng thái đăng nhập, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về tài khoản, thông tin xác thực, mã thông báo, cookie, quyền hay đăng nhập. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm xác thực, cấp mã thông báo hay truy cập của ứng dụng.

### Trợ lý đăng nhập tài khoản Microsoft

Xem lại lịch sử bộ môi giới tài khoản đã được làm sạch nội dung và tính khả dụng chính xác của Trợ lý đăng nhập tài khoản Microsoft. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các yêu cầu đăng nhập tài khoản Microsoft hiện có có thể tiếp tục theo chính sách hiện tại. Sự kiện trong quá khứ không thể cho phép khôi phục hay xác lập trạng thái tài khoản hiện tại. Tài khoản, thông tin xác thực, mã thông báo, đăng ký thiết bị, dữ liệu ứng dụng, quyền, trạng thái đăng nhập, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về tài khoản, thông tin xác thực, mã thông báo, đăng ký, quyền hay đăng nhập. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm xác thực, kết nối tài khoản hay truy cập của ứng dụng.
