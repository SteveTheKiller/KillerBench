# Sửa tệp hệ thống

Dùng hướng dẫn này khi các thành phần Windows bị hỏng: tính năng không cài được, bản cập nhật báo lỗi thành phần hoặc công cụ hệ thống bị treo. KillerBench chỉ chạy DISM và SFC sau khi kiểm tra trực tiếp xác nhận có hỏng hóc, sau đó xác minh kết quả.

## Trước khi bắt đầu

- Chạy KillerBench với quyền quản trị viên.
- Hoàn tất mọi lần khởi động lại đang chờ trước.
- Nếu máy được quản lý, hãy kiểm tra xem chính sách có trỏ DISM đến một nguồn sửa chữa cụ thể không. KillerBench báo cáo chính sách đó và không thay đổi nó.

## Kiểm tra hỏng hóc

1. Mở **Sửa chữa** và chọn **Tệp hệ thống**.
2. Để **Tự động sửa các vấn đề đã xác nhận** tắt và nhấn **Chạy chẩn đoán**.
3. Đọc báo cáo. Báo cáo kiểm kê trạng thái của gói bảo trì, tính năng tùy chọn và khả năng, kiểm tra điều kiện tiên quyết và quét các tệp hệ thống được bảo vệ. Kết quả sạch nghĩa là không có sửa chữa nào được đề xuất.

## Sửa hỏng hóc đã xác nhận

1. Bật **Tự động sửa các vấn đề đã xác nhận** và nhấn **Chạy sửa chữa**. KillerBench chỉ chọn DISM hoặc SFC khi kiểm tra trực tiếp xác nhận có hỏng hóc.
2. Chờ xác minh. Báo cáo chỉ hiển thị **Đã xác minh** khi kiểm tra tiếp theo đọc lại được trạng thái đã sửa.
3. Nếu báo cáo cho biết kho không thể khôi phục hoặc nguồn sửa chữa không khả dụng, hãy dừng ở đó. Lặp lại sửa chữa sẽ không thay đổi kết quả.

Một lần chạy DISM hoặc SFC bị gián đoạn sẽ được đối chiếu ở lần khởi động tiếp theo mà không lặp lại lệnh bảo trì.

## Dọn dẹp kho thành phần

Chọn **Dọn dẹp kho thành phần** trong **Cập nhật**. Công cụ trước tiên hỏi Windows xem có nên dọn dẹp không. Bảo trì tùy chọn sẽ xóa các thành phần đã bị thay thế và phân tích lại. Công cụ không bao giờ dùng ResetBase và không bao giờ xóa thủ công nội dung WinSxS.

## Từ dòng lệnh

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Xem [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) và [servicing-logs](cli.html#cmd-servicing-logs). `repair` xem trước theo mặc định và chỉ thực hiện khi có `--execute`.

## Liên quan

- [Sửa Windows Update](fix-windows-update.html)
- [Mục Sửa chữa](repair.html#system-files)
- [Cách giữ an toàn khi thay đổi](safety.html)
