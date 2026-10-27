# Khắc phục sự cố in ấn

Dùng hướng dẫn này khi tác vụ in nằm mãi trong hàng đợi, máy in hiển thị ngoại tuyến hoặc máy in mặc định bị sai. KillerBench đọc bộ đệm in, hàng đợi, tác vụ, trình điều khiển và cổng trước, sau đó cho phép bạn sửa đúng một hàng đợi hoặc một tác vụ.

## Kiểm tra máy in

1. Mở **Thiết bị** và chọn **In ấn**.
2. Nhấn **Tìm máy in cục bộ** và chọn hàng đợi đang gặp sự cố. Để kiểm tra mọi hàng đợi, chọn **Sử dụng các kiểm tra in ấn chung**.
3. Để **Tự động sửa các vấn đề đã xác nhận** tắt và nhấn **Chạy chẩn đoán**.
4. Đọc báo cáo. Báo cáo hiển thị trạng thái Print Spooler, hàng đợi bị tạm dừng, chế độ ngoại tuyến, tác vụ hiển thị, tình trạng trình điều khiển, đăng ký cổng, lỗi PrintService gần đây và, với máy in mạng, một lần thử kết nối đến địa chỉ được gán của máy in.

Một kết nối thành công hay một mã trạng thái sạch không chứng minh máy in có giấy, mực hoặc cơ cấu hoạt động tốt.

## Sửa hàng đợi

1. Chạy lại **In ấn** với **Tự động sửa các vấn đề đã xác nhận** được bật. Công cụ có thể khởi động bộ đệm in đang bật, tiếp tục đúng một hàng đợi bị tạm dừng có tác vụ đang chờ, và tắt chế độ ngoại tuyến.
2. Tắt chế độ ngoại tuyến có thể giải phóng các tác vụ đang chờ, vì vậy hãy chắc chắn rằng các tác vụ đó vẫn cần được in.
3. Một hàng đợi bị ai đó cố ý tạm dừng mà không có gì đang chờ sẽ vẫn ở trạng thái tạm dừng.

## Xóa một tác vụ bị kẹt

1. Nhấn **Tìm các tác vụ in bị chặn**.
2. Chọn tác vụ hiển thị Error hoặc Blocked và đánh dấu **Chỉ hủy công việc đã chọn này**.
3. Nhấn **Chạy chẩn đoán**. Chỉ tác vụ đó bị hủy và kết quả được đọc lại.

## Sửa máy in mặc định

Chọn hàng đợi cần làm mặc định và đánh dấu **Sửa máy in mặc định bị thiếu hoặc xung đột bằng hàng đợi này**, rồi nhấn **Chạy chẩn đoán**. Thao tác này chỉ thực hiện khi máy in mặc định bị thiếu hoặc xung đột.

## Từ dòng lệnh

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Xem [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) và [default-printer-set](cli.html#cmd-default-printer-set).

## Liên quan

- [Mục Thiết bị](devices.html#printing)
- [Khắc phục sự cố mạng và internet](fix-network.html)
