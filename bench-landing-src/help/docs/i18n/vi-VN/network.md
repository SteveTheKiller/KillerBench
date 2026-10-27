# Mạng

Mạng kiểm tra kết nối, DNS, tuyến đường, đăng nhập không dây và có dây, chia sẻ tệp và truy cập từ xa. Các công cụ kết nối tới máy khác dùng mục tiêu và thời gian chờ bạn đặt, và không có gì thay đổi trừ khi bạn xác nhận một sửa chữa.

Hướng dẫn từng bước: [Khắc phục sự cố mạng và internet](fix-network.html).

## Công cụ trong mục này

Kiểm tra kết nối, DNS, tuyến đường, mạng không dây, chia sẻ và truy cập từ xa.

### Mạng

Kiểm tra bộ điều hợp, trạng thái và nguồn gốc địa chỉ, cổng mặc định, cấu hình DNS, chính sách không gian tên DNS hiệu lực, cài đặt TCP, cấu trúc danh mục giao thức và nhà cung cấp không gian tên Winsock gốc, bảng định tuyến IP và ánh xạ trong tệp Hosts. Tự động sửa có thể khởi động các dịch vụ DNS Client, DHCP Client, máy khách SMB, Network Location Awareness, Network List và IP Helper đang bật, và gia hạn thuê cho các bộ điều hợp DHCP vật lý đã kết nối nhưng thiếu địa chỉ IPv4 dùng được. Khởi động các dịch vụ nhận dạng mạng có thể tính toán lại danh tính mạng và áp dụng một hồ sơ tường lửa hiện có khác. IP Helper có thể kích hoạt các tính năng chuyển tiếp IPv6, proxy cổng và thông báo mạng hiện có. Việc khôi phục dịch vụ xác minh trạng thái đang chạy, không xác minh phân giải tên thành công, chọn tuyến, nhận dạng mạng hay hoạt động của đường hầm. Tuyến đường, mục Hosts, máy chủ DNS, hồ sơ, quy tắc tường lửa, nhà cung cấp Winsock, cấu hình chuyển tiếp, kiểu khởi động và chính sách được giữ nguyên. Việc gia hạn có thể thay đổi địa chỉ được thuê và làm gián đoạn kết nối. Cài đặt tĩnh và bộ điều hợp ảo được giữ nguyên. Phản hồi ICMP chính xác từ cổng mặc định không xác minh khả năng truy cập internet.

### Mạng không dây

Kiểm tra thiết bị mạng và tính khả dụng của WLAN AutoConfig. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc; các hồ sơ đã lưu có thể tự động kết nối lại. Hồ sơ, thông tin xác thực, chính sách, cài đặt sóng vô tuyến và kiểu khởi động được giữ nguyên. Không đặt lại bộ điều hợp và không cài trình điều khiển. Việc xác minh bao gồm trạng thái đang chạy, không bao gồm xác thực không dây hay kết nối.

### Xác thực có dây (802.1X)

Kiểm tra thiết bị mạng và Wired AutoConfig cho các mạng yêu cầu 802.1X. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc. Các hồ sơ xác thực hiện có có thể được kích hoạt và thay đổi quyền truy cập mạng. Chứng chỉ, thông tin xác thực, hồ sơ, chính sách và cài đặt khởi động được giữ nguyên. Việc xác minh chỉ bao gồm trạng thái đang chạy, không bao gồm xác thực hay kết nối.

### Kết nối mạng

Xem lại có giới hạn việc gán hồ sơ mạng hiện tại, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của dịch vụ Network Connections. Bằng chứng chưa đầy đủ được ưu tiên. Hồ sơ công cộng và lỗi trong quá khứ chỉ mang tính ngữ cảnh và không thể cho phép khôi phục hay thay đổi mạng. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các giao diện quản lý mạng hiện có có thể tiếp tục. Bộ điều hợp, địa chỉ, tuyến đường, cài đặt DNS, hồ sơ kết nối, thông tin xác thực, cấu hình tường lửa, chính sách và kiểu khởi động được giữ nguyên. Không có kết nối hay cài đặt mạng nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hoạt động của bộ điều hợp, việc gán địa chỉ, định tuyến, phân giải tên hay kết nối.

### Proxy tự động WinHTTP

Xem lại giá trị mặc định WinHTTP đã lưu, cài đặt Internet của tài khoản hiện tại, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của WinHTTP Auto-Proxy. Bằng chứng chưa đầy đủ được ưu tiên. Cấu hình proxy và lỗi trong quá khứ không thể cho phép khôi phục dịch vụ hay việc phát hiện. Một dịch vụ khởi động theo yêu cầu đã dừng có thể là bình thường. Khi đúng công cụ này được chọn, khôi phục tùy chọn chỉ có thể khởi động dịch vụ đang bật đã dừng sau khi kiểm tra phụ thuộc mới. Các ứng dụng hiện có có thể tiếp tục phát hiện WPAD hoặc PAC theo cài đặt hiện tại. Định nghĩa proxy, URL PAC, thông tin xác thực, cài đặt mạng, cấu hình tường lửa, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có URL nào được tải và không có cài đặt proxy hay mạng nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc phát hiện, khả năng truy cập, xác thực, quyền sở hữu chính sách hay việc ứng dụng sử dụng.

### Trình quản lý kết nối Windows

Xem lại có giới hạn việc gán hồ sơ mạng hiện tại, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của Windows Connection Manager. Bằng chứng chưa đầy đủ được ưu tiên. Hồ sơ công cộng và lỗi trong quá khứ chỉ mang tính ngữ cảnh và không thể cho phép khôi phục hay quyết định kết nối. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Windows có thể tiếp tục các quyết định kết nối tự động theo hồ sơ và chính sách hiện có. Bộ điều hợp, hồ sơ, thông tin xác thực, cài đặt đo lưu lượng, chi phí, chuyển vùng, tường lửa, VPN, xác thực và kiểu khởi động được giữ nguyên. Không có hồ sơ, bộ điều hợp hay cài đặt nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khả năng áp dụng hồ sơ, chọn kết nối, xác thực, định tuyến, phân giải tên hay kết nối.

### Dịch vụ thiết lập mạng

Xem lại có giới hạn các liên kết giao thức hiện tại của bộ điều hợp, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của Network Setup Service. Bằng chứng chưa đầy đủ được ưu tiên. Trạng thái liên kết và lỗi trong quá khứ không thể cho phép khôi phục dịch vụ hay thay đổi thiết lập. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Việc thiết lập mạng đang chờ hiện có có thể tiếp tục theo chính sách Windows. Bộ điều hợp, trình điều khiển, gói trình điều khiển, liên kết, địa chỉ, tuyến đường, cài đặt DNS, hồ sơ, thông tin xác thực, cấu hình tường lửa, chính sách và kiểu khởi động được giữ nguyên. KillerBench không chọn thay đổi trình điều khiển hay cấu hình nào. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm tính khả dụng của bộ điều hợp, sự phù hợp của trình điều khiển, việc hoàn tất thiết lập, gán địa chỉ, định tuyến, phân giải tên hay kết nối.

### Chia sẻ tệp (SMB)

Kiểm tra tính khả dụng của máy chủ SMB của Windows. Khôi phục tùy chọn chỉ khởi động dịch vụ Server đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các chia sẻ tệp và máy in hiện có có thể truy cập được theo quyền và chính sách tường lửa hiện tại. Không có chia sẻ, quyền, cài đặt SMB, quy tắc tường lửa, thông tin xác thực, chính sách hay kiểu khởi động nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm truy cập của máy khách hay truyền tệp.

### Trình quản lý kết nối truy cập từ xa

Xem lại có giới hạn cấu hình hồ sơ VPN của tài khoản hiện tại, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của Remote Access Connection Manager mà không thu thập bí mật. Danh sách trống có thể là có chủ ý, và sự hiện diện của hồ sơ, nhãn kết nối hay lỗi trong quá khứ không thể cho phép khôi phục hay thử kết nối. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các yêu cầu quay số và VPN hiện có có thể tiếp tục. Hồ sơ kết nối, thông tin xác thực, tuyến đường, bộ điều hợp, cài đặt xác thực, chính sách và kiểu khởi động được giữ nguyên. Không có kết nối nào được khởi tạo. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khả năng áp dụng hồ sơ, khả năng truy cập máy chủ, xác thực, thương lượng đường hầm, cài đặt tuyến, phân giải tên hay kết nối.

### Dịch vụ Máy tính Từ xa

Xem lại trạng thái bật Remote Desktop cục bộ, Network Level Authentication và cổng trình nghe, lịch sử kết nối và phiên có giới hạn, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của dịch vụ. Bằng chứng chưa đầy đủ được ưu tiên. Bản ghi trong quá khứ không thể cho phép khôi phục dịch vụ hay thay đổi cấu hình. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Một trình nghe hiện có có thể truy cập được theo chính sách, NLA, quy tắc tường lửa và quyền hiện tại. Trạng thái bật Remote Desktop, trình nghe, chứng chỉ, cổng, quy tắc tường lửa, NLA, quyền, phiên, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có trình nghe, quy tắc tường lửa, quyền truy cập tài khoản, chứng chỉ hay phiên nào được tạo hoặc thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm chính sách hiệu lực, liên kết trình nghe, truy cập qua tường lửa, danh tính chứng chỉ, quyền, xác thực, khả năng truy cập mạng, kết nối thành công hay việc tạo phiên.

### Quản lý từ xa Windows

Xem lại có giới hạn cấu hình trình nghe WinRM, lịch sử hoạt động, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của dịch vụ. Bằng chứng chưa đầy đủ được ưu tiên. Trình nghe bị tắt hoặc vấn đề hiện tại của dịch vụ cần được xem lại, còn bản ghi trong quá khứ không thể cho phép khôi phục hay thay đổi cấu hình. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các trình nghe WS-Management hiện có có thể truy cập được theo cài đặt xác thực, chứng chỉ, tường lửa, TrustedHosts và chính sách hiện tại. Trình nghe, địa chỉ, cổng, chứng chỉ, xác thực, mã hóa, TrustedHosts, quy tắc tường lửa, quyền, hạn ngạch, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có trình nghe, quy tắc tường lửa, mục tin cậy, chứng chỉ, thông tin xác thực hay phiên từ xa nào được tạo hoặc thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm chính sách hiệu lực, liên kết trình nghe, truy cập qua tường lửa, hiệu lực chứng chỉ, xác thực, quyền, khả năng truy cập của máy khách hay thực thi lệnh từ xa.
