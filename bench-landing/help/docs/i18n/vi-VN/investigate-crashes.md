# Điều tra sự cố treo máy và màn hình xanh

Dùng hướng dẫn này khi Windows khởi động lại bất ngờ, hiển thị lỗi dừng hoặc một ứng dụng liên tục bị treo. KillerBench gom bằng chứng về sự cố, tắt máy, lỗi phần cứng và độ tin cậy vào một báo cáo. Báo cáo mô tả những gì đã được ghi lại và không bao giờ đoán nguyên nhân gốc.

## Kiểm tra máy tính

1. Mở **Chẩn đoán** và chọn **Chỉ số chính**.
2. Nhấn **Chạy chẩn đoán**. Báo cáo kết hợp bộ nhớ, tình trạng đĩa, sự cố thiết bị, lịch sử Reliability Monitor, bản ghi lỗi phần cứng, báo cáo sự cố, các lần tắt máy bất ngờ và kết quả Windows Memory Diagnostic.
3. Đọc kết luận và bằng chứng bên dưới. Các sự cố trong quá khứ giải thích điều đã xảy ra; chúng không chứng minh lỗi vẫn còn.

## Thu hẹp phạm vi

- **Lịch sử lỗi phần cứng** đọc bản ghi WHEA và chẩn đoán bộ nhớ cùng với sự cố thiết bị và tình trạng đĩa.
- **Lịch sử độ tin cậy** nhóm các sự cố ứng dụng, tình trạng treo và lần tắt máy bất ngờ trong 14 ngày gần nhất.
- **Lỗi ứng dụng** tập trung vào các chương trình bị treo.
- **Lịch sử cài đặt trình điều khiển** hiển thị các lần cài trình điều khiển thất bại được đối chiếu với thiết bị hiện có.
- **Sự cố thiết bị** liệt kê các thiết bị có mã lỗi của Trình quản lý Thiết bị.

## Đọc tệp kết xuất sự cố

KillerBench liệt kê cấu hình kết xuất và siêu dữ liệu tệp kết xuất, và có thể phân tích cấu trúc minidump để lấy ngoại lệ và tên mô-đun. Nội dung kết xuất không bao giờ được mở trong ứng dụng máy tính. Khi đã tải gói tri thức bug-check, mã dừng sẽ được giải thích theo ý nghĩa đã được ghi trong tài liệu.

Giữ các tệp kết xuất cho đến khi điều tra xong. **Dọn dẹp** có thể xóa chúng, nhưng như vậy sẽ loại bỏ bằng chứng.

## Từ dòng lệnh

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Xem [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) và [device-code](cli.html#cmd-device-code).

## Liên quan

- [Mục Chẩn đoán](diagnostics.html#vitals)
- [Báo cáo và bằng chứng](reports.html)
