# Lưu trữ

Kiểm tra đĩa, ổ đĩa, Không gian lưu trữ và các dịch vụ bảo trì ổ đĩa. Chẩn đoán không chọn thao tác nào trên đĩa; mọi thao tác được hỗ trợ có thay đổi máy tính đều cần mục tiêu chính xác, điều kiện tiên quyết và xác nhận riêng.

Hướng dẫn từng bước: [Kiểm tra và sửa ổ đĩa](check-a-drive.html) và [Giải phóng dung lượng ổ đĩa](free-disk-space.html).

## Công cụ trong mục này

### Lưu trữ

Ánh xạ có giới hạn danh tính của đĩa, phân vùng và ổ đĩa có ký tự, đánh giá áp lực dung lượng trống 10% trên mọi ổ đĩa có ký tự, kiểm tra phân bổ bộ nhớ bản sao bóng VSS và giá trị hạn ngạch mặc định của từng ổ đĩa, tóm tắt tiến trình hiện tại của các tác vụ mô-đun Storage, kiểm tra ổ đĩa NTFS đã chọn và trạng thái tự phục hồi trực tuyến của nó, và kiểm tra các thiết bị đĩa và bộ điều khiển trên PC này. Ngưỡng dung lượng, cài đặt hạn ngạch và trạng thái tác vụ là bằng chứng để xem lại và không chọn thao tác dọn dẹp, đổi kích thước, thay đổi hạn ngạch hay điều khiển tác vụ. Ánh xạ ký tự ổ đĩa phản ánh một thời điểm và không bao gồm mọi đường dẫn gắn kết. Kết luận về thiết bị không được ánh xạ tới ổ đĩa đã chọn. Ổ đĩa Windows là mặc định. Không có tệp, ảnh chụp nhanh, tác vụ hay trình điều khiển nào bị thay đổi, không có cài đặt nào bị thay đổi, không có ổ đĩa nào bị tháo gắn kết và không có lần khởi động lại nào được lên lịch.

### Không gian lưu trữ

Kiểm kê các nhóm và đĩa ảo Không gian lưu trữ không phải nguyên thủy, bao gồm tình trạng, trạng thái hoạt động, dung lượng, phân bổ và khả năng phục hồi, sau đó trả về một kết luận hoạt động có giới hạn. Windows có thể khởi động theo yêu cầu nhà cung cấp quản lý Không gian lưu trữ trong khi trả lời truy vấn chỉ đọc này. Nhóm, đĩa ảo, đĩa vật lý, ổ đĩa, khả năng phục hồi, phân bổ, cài đặt bảo trì, chính sách và kiểu khởi động được giữ nguyên. Không có thao tác nào trên nhóm, đĩa, ổ đĩa, phân bổ, gắn kết, sửa chữa, tối ưu hóa hay cài đặt được yêu cầu. Kết quả của nhà cung cấp không xác minh khả năng truy cập nhóm, tính dự phòng, tính toàn vẹn dữ liệu, khả năng khôi phục từ bản sao lưu hay I/O của ứng dụng.

### Dịch vụ lưu trữ

Xem lại có giới hạn các tác vụ mô-đun Storage hiện tại, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của Dịch vụ lưu trữ. Bằng chứng chưa đầy đủ được ưu tiên. Trạng thái tác vụ và lỗi trong quá khứ không thể cho phép khôi phục dịch vụ hay thay đổi lưu trữ. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các thông báo lưu trữ hiện có và hoạt động lưu trữ do chính sách quản lý có thể tiếp tục. Ổ đĩa, phân vùng, hệ thống tệp, điểm gắn kết, thiết bị, trình điều khiển, cài đặt Storage Sense, chính sách bảo trì, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào trên tác vụ, ổ đĩa, phân vùng, điểm gắn kết, tệp, thiết bị, trình điều khiển hay cài đặt lưu trữ. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm kết quả tác vụ hay hoạt động của thiết bị, ổ đĩa hoặc chính sách lưu trữ.

### Dịch vụ Tối ưu hóa ổ đĩa

Kiểm tra tính khả dụng của dịch vụ Tối ưu hóa ổ đĩa. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Hoạt động tối ưu hóa do Windows lên lịch hiện có có thể tiếp tục theo chính sách bảo trì hiện tại. Ổ đĩa, hệ thống tệp, dữ liệu, lịch tối ưu hóa, chính sách bảo trì, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn ổ đĩa, không phân tích, trim, chống phân mảnh, tối ưu hóa hay lên lịch cho bất kỳ ổ đĩa nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm phân tích, tối ưu hóa hay tình trạng thiết bị.
