# Lần chẩn đoán đầu tiên

Bắt đầu với [Tải xuống và thiết lập](setup.html), sau đó chạy Quét toàn bộ hệ thống. Thao tác này thu thập bằng chứng chẩn đoán và lưu một nhật ký cục bộ; nó không chạy sửa chữa. Một kết luận mô tả phạm vi đã kiểm tra, không phải mọi lỗi có thể có trên máy tính.

## Chạy quét

1. Mở KillerBench. Mục **Chẩn đoán** mở ra với **Quét toàn bộ hệ thống** được chọn sẵn.
2. Để **Tự động sửa các vấn đề đã xác nhận** ở trạng thái tắt cho lần chạy đầu tiên.
3. Nhấn **Chạy chẩn đoán**. Tiến trình xuất hiện trong **Đầu ra thô** ở bên phải khi từng kiểm tra chạy.
4. Khi hoàn tất, ngăn đầu ra chuyển sang **Báo cáo dễ đọc**.

Bắt đầu mà không nâng quyền. Một số bằng chứng của Windows, như trình bảo vệ BitLocker và các dịch vụ được bảo vệ, cần quyền quản trị viên. Dưới tài khoản tiêu chuẩn, các kiểm tra đó báo cáo bằng chứng không khả dụng. Nếu bạn cần bằng chứng đó, hãy hoàn tất lần chạy hiện tại, giữ lại báo cáo, rồi chủ động mở lại KillerBench bằng **Chạy với tư cách quản trị viên**. Việc nâng quyền không chọn hay phê duyệt sửa chữa nào.

## Đọc báo cáo

Báo cáo bắt đầu bằng số lượng **Cần xem lại**, **Sửa chữa đã xác minh**, **Chưa hoàn tất** và **Đã kiểm tra**, sau đó là bước tiếp theo được đề xuất.

- **Những gì cần chú ý** liệt kê các vấn đề mà bằng chứng hỗ trợ, quan trọng nhất trước.
- **Thông tin đã thu thập** cho thấy những gì đã được đọc, kể cả bằng chứng chưa đầy đủ hoặc không khả dụng.
- Các sự kiện trong quá khứ được gắn nhãn là lịch sử. Một lỗi từ tháng trước không được coi là lỗi hiện tại.

Bằng chứng bị thiếu không bao giờ được tính là khỏe mạnh. Nếu một kiểm tra không chạy được, báo cáo sẽ nói rõ.

## Tự chọn kiểm tra

Mở **Chọn kiểm tra (nâng cao)** để chọn từng kiểm tra riêng thay cho lần quét toàn bộ. Mỗi mục trong ứng dụng cũng có các công cụ chuyên biệt, được mô tả trong các trang **Các mục** của hướng dẫn này.

## Sửa những gì tìm thấy

Khi báo cáo đề xuất một bản sửa, nó nêu tên mục và công cụ cần mở. Đọc [Cách giữ an toàn khi thay đổi](safety.html) trước lần sửa chữa đầu tiên. Việc xác thực trên máy thật vẫn đang tiến hành; một lần chẩn đoán thành công không có nghĩa mọi sửa chữa được đề xuất đã được kiểm thử trên bản dựng Windows của bạn. Hãy giữ bản sao lưu và xem lại chính xác mục tiêu cùng hệ quả trước mọi thay đổi.
