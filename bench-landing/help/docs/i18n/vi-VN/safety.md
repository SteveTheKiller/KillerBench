# Cách giữ an toàn khi thay đổi

KillerBench coi chẩn đoán và thay đổi là hai việc khác nhau. Chẩn đoán chỉ đọc. Quét toàn bộ hệ thống không bao giờ chạy sửa chữa. Hãy chọn một sửa chữa hoặc thao tác cụ thể, xem lại tùy chọn và điều kiện tiên quyết của nó, rồi xác nhận thay đổi được đề xuất.

## Trước khi thay đổi

1. **Bằng chứng mới.** Công cụ đọc lại trạng thái hiện tại. Một báo cáo đã lưu hay một sự kiện cũ không bao giờ cho phép thay đổi.
2. **Đủ điều kiện chính xác.** Thay đổi chỉ được đề xuất khi bằng chứng khớp đúng vấn đề mà nó khắc phục. Bất cứ điều gì chưa đầy đủ đều chặn thay đổi.
3. **Xác nhận của bạn.** Xem lại thao tác đã chọn trong hộp thoại xác nhận của nó. Một số thao tác, như giải mã ổ đĩa, cần xác nhận lần thứ hai. Hộp thoại theo chủ đề đang hoạt động của ứng dụng; giao diện của nó không thay đổi yêu cầu của thao tác.
4. **Điều kiện tiên quyết.** Quyền quản trị viên, trạng thái khởi động lại và quyền sở hữu thông qua chính sách hoặc quản lý thiết bị đều được kiểm tra. Các cài đặt do chính sách sở hữu được giữ nguyên.
5. **Bản ghi chờ.** KillerBench ghi lại những gì sắp làm trước khi yêu cầu Windows thực hiện.
6. **Kiểm tra lại lần cuối.** Mọi thứ được kiểm tra lại ngay trước khi gửi yêu cầu, nên một thay đổi xảy ra trong lúc đó sẽ dừng thao tác.

## Sau khi thay đổi

- KillerBench đọc lại kết quả và chỉ báo cáo **Đã xác minh** khi Windows cho thấy trạng thái mong đợi.
- Nếu Windows báo thành công nhưng trạng thái không thay đổi, kết quả sẽ nói rõ và không có gì được lặp lại.
- Mỗi thay đổi được lưu trong một nhật ký tại `%LOCALAPPDATA%\KillerBench`.

## Nếu có gì đó bị gián đoạn

Các thao tác được hỗ trợ có ghi nhật ký sẽ giữ một bản ghi chờ khi yêu cầu có thể đã đến Windows nhưng việc xác minh chưa hoàn tất. Dùng đường đối chiếu được ghi trong tài liệu của thao tác, hoặc lệnh `-recover` tương ứng, để kiểm tra bản ghi đó. Đối chiếu không phải là hoàn tác tự động. Hãy giữ nhật ký khi kết quả không chắc chắn và không tự lặp lại thay đổi. Hành vi khi gặp sự cố, khi hủy và ở lần khởi động sau vẫn cần được xác thực trên máy thật cho từng thao tác.

## Những gì KillerBench sẽ không làm

- Xóa TPM, xóa trình bảo vệ mà nó không tạo ra, hoặc hiển thị mật khẩu khôi phục.
- Thay đổi cài đặt do chính sách nhóm hoặc quản lý thiết bị kiểm soát.
- Chạy sửa chữa chỉ vì một sự kiện lịch sử hoặc báo cáo được nhập có nhắc đến vấn đề.

Chẩn đoán và phân tích báo cáo chạy cục bộ. Một thao tác bạn chọn có thể kết nối với đích bên ngoài, bao gồm dịch vụ tải phần mềm hoặc sao lưu khóa khôi phục lên Active Directory hay Entra ID. Hãy xem lại mục tiêu trước khi xác nhận. Ảnh chụp đã lưu và bằng chứng thô có thể chứa danh tính máy và đường dẫn nguồn, vì vậy hãy xem lại trước khi chia sẻ.

## Tình trạng phát triển

Phạm vi sửa chữa vẫn đang mở rộng. Một bản dựng thành công hay một kiểm tra mô phỏng không chứng minh hành vi sửa chữa trên máy thật. Tệp `VM-VALIDATION.md` của kho mã theo dõi các kịch bản trên máy Windows dùng một lần và phần xác thực còn lại.
