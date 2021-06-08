export function handle(exitCode, loginUrl) {
    if (loginUrl) {
        handleRemoteSessionExit(exitCode); window.location.href = loginUrl;
    } else {
        handleRemoteSessionExit(exitCode);
    }
}