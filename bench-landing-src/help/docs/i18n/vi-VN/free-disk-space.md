# Giải phóng dung lượng ổ đĩa

Dùng hướng dẫn này khi ổ đĩa sắp đầy. KillerBench cho thấy dung lượng đang đi đâu, xem trước những gì mỗi mục tiêu dọn dẹp sẽ xóa, và chỉ xóa sau khi bạn bật dọn dẹp vĩnh viễn.

## Xem còn bao nhiêu dung lượng

1. Mở **Lưu trữ** và chọn **Lưu trữ**.
2. Nhấn **Chạy chẩn đoán**. Báo cáo đánh dấu mọi ổ đĩa có ký tự còn dưới 10% dung lượng trống và hiển thị phân bổ bản sao bóng, giá trị hạn ngạch mặc định và chính sách Cảm biến Lưu trữ.
3. Với tệp kết xuất sự cố và nội dung cập nhật cũ, hãy chạy thêm **Chỉ số chính** trong **Chẩn đoán** và **Windows Update** trong **Cập nhật**; kích thước bộ nhớ đệm cập nhật xuất hiện ở đó.

## Xem trước việc dọn dẹp

1. Mở **Bảo trì** và chọn **Dọn dẹp**.
2. Đánh dấu các mục tiêu bạn muốn, như **Tệp tạm thời của người dùng hiện tại**, **Tệp tạm thời của Windows**, **Bộ nhớ đệm trình duyệt của người dùng hiện tại**, **Bộ nhớ đệm Tối ưu hóa Phân phối** hoặc **Thùng rác người dùng hiện tại**. Di chuột lên một mục tiêu để đọc chính xác các thư mục mà nó bao gồm.
3. Đặt **Tuổi tối thiểu (ngày)** và **Số tệp tối đa cho mỗi mục tiêu**.
4. Nhấn **Chạy chẩn đoán** khi **Thực hiện dọn dẹp vĩnh viễn đã chọn sau khi kiểm tra mới** đang tắt. Báo cáo liệt kê các tệp ứng viên và dung lượng ước tính cho mỗi mục tiêu. Không có gì bị xóa.

## Xóa tệp

1. Bật **Thực hiện dọn dẹp vĩnh viễn đã chọn sau khi kiểm tra mới** và nhấn **Chạy chẩn đoán**.
2. KillerBench kiểm tra lại từng tệp ứng viên trước khi xóa, bỏ qua các tệp mới, được bảo vệ hoặc đang dùng, và ghi từng kết quả vào nhật ký.

Dọn dẹp vĩnh viễn bỏ qua Thùng rác và không thể hoàn tác. Xóa báo cáo lỗi và tệp kết xuất sự cố sẽ loại bỏ bằng chứng bạn có thể cần để điều tra sự cố, vì vậy hãy xem lại chúng trước.

## Xóa bản cài đặt Windows trước đó

Sau một bản nâng cấp tính năng, **Bản cài đặt Windows trước đó** trong **Dọn dẹp** có thể xóa bản sao hoàn nguyên thông qua yêu cầu xóa khả năng hoàn nguyên được Windows hỗ trợ. Thao tác này chạy riêng, cần **Tôi đã sao lưu bất kỳ tệp và cài đặt nào tôi cần từ cài đặt trước đó**, và xóa vĩnh viễn khả năng quay lại phiên bản trước.

## Từ dòng lệnh

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Xem [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) và [previous-windows](cli.html#cmd-previous-windows).

## Liên quan

- [Mục Bảo trì](maintenance.html#cleanup)
- [Mục Lưu trữ](storage.html)
- [Kiểm tra và sửa ổ đĩa](check-a-drive.html)
