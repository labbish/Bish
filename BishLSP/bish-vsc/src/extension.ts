import { execSync } from "node:child_process";

import type { ExtensionContext } from 'vscode';
import vscode from 'vscode';
import type { LanguageClientOptions, ServerOptions } from 'vscode-languageclient/node';
import { LanguageClient } from 'vscode-languageclient/node';

let client: LanguageClient;
export async function activate(context: ExtensionContext) {
	try {
		execSync(`bish -h`);
	} catch {
		await vscode.window.showErrorMessage("Cannot find bish!");
		return;
	}

	const serverOptions: ServerOptions = {
		command: 'Bish',
		args: ['-l']
	};

	const clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'bish' }],
	};

	const runCommand = vscode.commands.registerCommand('bish.runFile', () => {
		const activeEditor = vscode.window.activeTextEditor;
		if (activeEditor) {
			const filePath = activeEditor.document.fileName;

			const terminal = vscode.window.createTerminal(`Bish Run`);
			terminal.show();
			terminal.sendText(`Bish -f "${filePath}"`);
		}
	});

	context.subscriptions.push(runCommand);

	client = new LanguageClient(
		'bishLanguageServer',
		'Bish Language Server',
		serverOptions,
		clientOptions
	);

	await client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) return undefined;
	return client.stop();
}