# Tải xuống và thiết lập

KillerBench 1.0.0 chưa được phát hành. Chưa có gói hay bản tải xuống KillerBench công khai nào. Trang này mô tả các yêu cầu hiện tại của gói để bạn có thể chuẩn bị mà không cần tải một bản sao không chính thức.

## Windows và môi trường chạy

- Mục tiêu phát hành hiện tại là **Windows x64**. Windows 11 x64 là nền tảng xác thực trên máy thật hiện tại. Không cung cấp bản phân phối gốc cho ARM64, x86 và Windows Server.
- KillerBench được đóng gói theo hai cách. Trình cài đặt **phụ thuộc framework** và cần **.NET Desktop Runtime 10 for Windows x64**. .NET Runtime thông thường, ASP.NET Core Runtime và .NET Framework không cung cấp WPF. Bản di động mang theo môi trường chạy riêng và không cần cài đặt riêng. Công cụ dòng lệnh `killerbench-cli.exe` cần .NET 10 Runtime for Windows x64. SDK chỉ cần cho việc phát triển, không cần cho việc sử dụng thông thường.
- Dùng phiên bản Windows được [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) hỗ trợ. Việc môi trường chạy được hỗ trợ không có nghĩa các sửa chữa của KillerBench đã được xác thực trên mọi phiên bản hay bản dựng được hỗ trợ. Phạm vi cho Windows 10 vẫn đang chờ và các phiên bản được hỗ trợ của nó bị giới hạn bởi yêu cầu vòng đời hiện tại của Microsoft.
- Chỉ lấy Desktop Runtime từ [trang tải xuống .NET 10 của Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), chọn **.NET Desktop Runtime**, **Windows**, **x64**. Tuân theo quy định cài đặt phần mềm của tổ chức bạn.

Nếu có `dotnet`, lệnh `dotnet --list-runtimes` sẽ liệt kê `Microsoft.WindowsDesktop.App 10.0.x` cho bản cài đặt x64. Một môi trường chạy chỉ được cài cho kiến trúc khác không đáp ứng gói cài đặt x64.

## Khi gói được phát hành

1. Dùng bản tải xuống chính thức được liên kết từ trang web này. Hiện chưa có liên kết tải xuống KillerBench nào hoạt động.
2. Đọc yêu cầu của gói đó, [giấy phép GPLv3](../license.txt), hướng dẫn về Mã nguồn tương ứng, thông báo bên thứ ba đi kèm, thông tin chữ ký và tổng kiểm. Chỉ tên tệp thôi không chứng minh tính xác thực.
3. Chạy trình cài đặt cho tài khoản hiện tại, hoặc cho mọi người dùng sau lời nhắc nâng quyền. Trình cài đặt thêm lối tắt trong menu Bắt đầu, lối tắt trên màn hình nền tùy chọn và một mục trong Thêm hoặc loại bỏ chương trình. Bản di động chạy mà không cần cài đặt.
4. Khởi động KillerBench bằng tài khoản thông thường và làm theo [Lần chẩn đoán đầu tiên](first-run.html). Các lỗi truy cập khi chẩn đoán vẫn được hiển thị; nâng quyền là một lựa chọn chủ động riêng.

Đừng bỏ qua cảnh báo bảo mật của Windows để chạy một bản sao chưa được xác minh. Hãy giữ bản sao lưu mới nhất trước khi chọn bất kỳ thao tác nào thay đổi máy tính.

Khi khởi động, KillerBench kiểm tra GitHub để tìm bản phát hành ổn định mới hơn và hỏi trước khi cài đặt. Bản tải xuống phải khớp với tổng kiểm SHA-256 đã công bố. Tắt việc kiểm tra bằng **Kiểm tra cập nhật khi khởi động** trong mục Giới thiệu.

## Giới hạn hiện tại

Bản dựng hiện tại có mười hai mục, chẩn đoán chỉ đọc, báo cáo và các bề mặt sửa chữa được chọn riêng lẻ. Việc xác thực sửa chữa trên máy thật vẫn đang tiến hành. Một giao diện hoạt động, bản dựng thành công hay kết quả dữ liệu thử không chứng minh hành vi sửa chữa trên máy tính thật. Xem [Cách giữ an toàn khi thay đổi](safety.html#development-status).

Bảng điều khiển minh họa trên trang giới thiệu là đầu ra tổng hợp. Gói công khai đầu tiên vẫn đang chờ.

## Mã nguồn, giấy phép và hỗ trợ

KillerBench được cấp phép theo GPLv3. Hướng dẫn cung cấp mã nguồn và quyền sử dụng phông chữ của bên thứ ba vẫn đang chờ cho gói công khai đầu tiên. Đọc [Về KillerBench](about.html) để biết về khả năng có mã nguồn, thông báo phân phối, quyền riêng tư của bằng chứng và thông tin hỗ trợ. [Hướng dẫn Engine](../engine/) dành cho nhà phát triển có bản sao mã nguồn được phép; nó không ngụ ý rằng đã có gói Engine công khai.
