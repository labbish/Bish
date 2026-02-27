import path from 'path';
import type { ExtensionContext } from 'vscode';
import vscode from 'vscode';
import type { LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';
import { LanguageClient } from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	const serverDll = path.join(__dirname, '../bin/BishLSP.dll');

	const serverOptions: ServerOptions = {
		run: {
			command: "dotnet",
			args: [serverDll],
			options: { cwd: path.dirname(serverDll) }
		},
		debug: {
			command: "dotnet",
			args: [serverDll],
			options: { cwd: path.dirname(serverDll) }
		}
	};

	const clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'bish' }],
	};

	const runCommand = vscode.commands.registerCommand('bish.runFile', () => {
		const activeEditor = vscode.window.activeTextEditor;
		if (activeEditor) {
			const runnerPath = path.join(__dirname, '../bin/Bish.dll');
			const filePath = activeEditor.document.fileName;

			const terminal = vscode.window.createTerminal(`Bish Run`);
			terminal.show();
			terminal.sendText(`dotnet ${runnerPath} -f "${filePath}"`);
		}
	});

	context.subscriptions.push(runCommand);

	client = new LanguageClient(
		'bishLanguageServer',
		'Bish Language Server',
		serverOptions,
		clientOptions
	);

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) return undefined;
	return client.stop();
}