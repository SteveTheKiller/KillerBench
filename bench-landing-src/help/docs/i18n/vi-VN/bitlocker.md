# BitLocker

BitLocker kiểm tra mã hóa ổ đĩa, trình bảo vệ và thông tin khôi phục, sau đó giúp bạn sao lưu, bảo vệ hoặc thay đổi một ổ đĩa. Công cụ chỉ hoạt động trên máy tính này.

Hướng dẫn từng bước: [Mã hóa ổ đĩa bằng BitLocker](encrypt-with-bitlocker.html).

## Sử dụng công cụ BitLocker

Chọn ký tự ổ đĩa và một **Thao tác**, rồi nhấn **Chạy chẩn đoán**.

- **Kiểm tra** đọc trạng thái mã hóa, tình trạng bảo vệ và danh tính các trình bảo vệ. Đây là thao tác mặc định và không thay đổi gì.
- **Sao lưu khôi phục** ghi mật khẩu khôi phục đã chọn vào một tệp mới nằm ngoài ổ đĩa được bảo vệ và xác minh nó. Nhấn **Tìm các trình bảo vệ khôi phục** trước, chọn trình bảo vệ, rồi xác nhận sao lưu.
- **Mã hóa** sao lưu mật khẩu khôi phục đã chọn, xác minh bản sao lưu, rồi bắt đầu mã hóa một lần với phương thức bạn chọn. Ổ đĩa Windows cần một trình bảo vệ TPM và một mật khẩu khôi phục. Ổ dữ liệu chỉ cần mật khẩu khôi phục.
- **Tiếp tục bảo vệ** bật lại bảo vệ cho một ổ đĩa đã được mã hóa hoàn toàn nhưng đang bị tạm ngưng bảo vệ.

Mỗi thao tác này đều cần hộp xác nhận riêng, và KillerBench kiểm tra lại ổ đĩa ngay trước khi thực hiện.

## Thao tác BitLocker khác

Mở **Thao tác BitLocker khác** cho các thay đổi nằm ngoài **Chạy chẩn đoán**. Chọn một thao tác, nhấn **Xem trước** để đọc chính xác những gì nó yêu cầu, rồi xác nhận và nhấn **Chạy thao tác**.

- **Bật tự động mở khóa** giúp một ổ dữ liệu đã mã hóa tự mở khóa khi Windows khởi động. Ổ đĩa Windows phải được mã hóa hoàn toàn và đang bật bảo vệ.
- **Thêm trình bảo vệ TPM** hoặc **Thêm mật khẩu khôi phục** tạo một trình bảo vệ mới. KillerBench không bao giờ hiển thị hay ghi nhật ký mật khẩu khôi phục mới, vì vậy hãy sao lưu nó ngay.
- **Sao lưu mật khẩu khôi phục vào Active Directory** hoặc **Microsoft Entra ID** gửi trình bảo vệ bạn đã chọn ở trên. KillerBench báo cáo rằng Windows đã chấp nhận yêu cầu. Công cụ không thể đọc bản sao trong thư mục, vì vậy hãy xác nhận nó ở đó.
- **Giải mã ổ đĩa này** bắt đầu giải mã, ví dụ trước khi mã hóa lại bằng một phương thức bắt buộc. Thao tác này cần xác nhận lần thứ hai vì sau đó ổ đĩa không còn được bảo vệ, và nó từ chối ổ đĩa Windows khi các ổ khác đang tự động mở khóa từ ổ này.

## TPM và FIPS

Nếu có TPM nhưng chưa sẵn sàng, mục Bảo mật có thể chuẩn bị nó mà không xóa. KillerBench không bao giờ xóa TPM, vì điều đó hủy các khóa mà BitLocker và Windows Hello đang giữ. Công tắc chính sách thuật toán FIPS có sẵn dưới dạng một sửa chữa tường minh, có thể đảo ngược, cho các tổ chức cần mã PIN BitLocker được FIPS phê duyệt.

## Công cụ trong mục này

Kiểm tra mã hóa, trình bảo vệ và thông tin khôi phục, rồi sao lưu chúng hoặc bảo vệ ổ đĩa.

### BitLocker

Xem trước các tùy chọn BitLocker và kiểm tra ổ đĩa đã chọn. Quy trình này không thực hiện mã hóa hay sao lưu khôi phục.
