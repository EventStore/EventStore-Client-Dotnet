
using System;
#if NET48
using Mono.Unix.Native;
#endif

namespace EventStore.Client {
	public class Utils {
		public static void DuplicateOutput(string outputFile) {
#if NET48
			int fd = Syscall.open(outputFile,
				 OpenFlags.O_CREAT | OpenFlags.O_APPEND | OpenFlags.O_RDWR,
				FilePermissions.S_IRUSR | FilePermissions.S_IWUSR);
			if (fd == -1) {
				throw new Exception($"Failed to open output file: {Syscall.GetLastError()}");
			}
			int err = Syscall.dup2(fd, 1);
			if (err == -1) {
				throw new Exception($"Failed to duplicate standard output stream: {Syscall.GetLastError()}");
			}

			err = Syscall.dup2(fd, 2);
			if (err == -1) {
				throw new Exception($"Failed to duplicate standard error stream: {Syscall.GetLastError()}");
			}
#else
			return;
#endif
		}
	}
}
