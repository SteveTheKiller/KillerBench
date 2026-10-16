# Khắc phục sự cố

## Một thao tác báo bị chặn

Kết quả bị chặn nghĩa là thao tác được yêu cầu không thể tiếp tục. Đọc thông báo và nhật ký để biết thao tác dừng trước khi gửi đi hay sau một bước trước đó. Đừng cho rằng một thao tác hoàn tất một phần đã được hoàn tác. Các lý do điều kiện tiên quyết thường gặp nhất được nêu dưới đây.

- **Cần quyền quản trị viên.** Đóng KillerBench và khởi động lại bằng **Chạy với tư cách quản trị viên**.
- **Trạng thái khởi động lại phải rõ ràng.** Windows đang chờ khởi động lại. Khởi động lại máy tính, rồi chạy lại thao tác.
- **Có chính sách được quản lý.** Chính sách nhóm hoặc quản lý thiết bị sở hữu cài đặt này. Hãy thay đổi nó ở đó.
- **Bằng chứng đã thay đổi.** Có điều gì đó thay đổi giữa lần kiểm tra đầu và lần kiểm tra lại cuối cùng. Chạy lại công cụ để đọc trạng thái mới.

## Một yêu cầu trước cần đối chiếu

KillerBench tìm thấy một bản ghi chờ từ một yêu cầu bị gián đoạn. Chạy lại cùng thao tác đó. Thao tác sẽ kiểm tra điều gì đã xảy ra và báo cáo kết quả thực mà không lặp lại yêu cầu. Trên dòng lệnh, dùng lệnh `-recover` tương ứng, ví dụ `killerbench-cli bitlocker-auto-unlock-recover`.

## Một kiểm tra hiển thị Chưa hoàn tất hoặc Không rõ

Không đọc được bằng chứng, thường là vì KillerBench không chạy với quyền quản trị viên hoặc một dịch vụ Windows không khả dụng. KillerBench báo cáo điều này một cách trung thực thay vì cho rằng máy tính khỏe mạnh.

## Tệp được lưu ở đâu

Nhật ký, bản ghi chờ và bằng chứng đã lưu nằm trong `%LOCALAPPDATA%\KillerBench`. Mỗi thao tác được bảo vệ có thư mục riêng. Hãy giữ chúng khi bạn cần hồ sơ về những gì đã thay đổi.

## Báo cáo sự cố

KillerBench đang được phát triển. Khi báo cáo sự cố, hãy đính kèm báo cáo đã xuất và nhật ký hoạt động, đồng thời xóa mọi thứ bạn không muốn chia sẻ.
