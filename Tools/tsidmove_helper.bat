@echo �\��t�@�C���Ɋ܂܂��TransportStreamID�̏���ύX���܂��B
@echo �`�����l���ĕ҂Ȃǂ�TransportStreamID���ύX���ꂽ�Ƃ��Ɏg���܂��B
@echo ChSet4.txt��ChSet5.txt�͂��炩���߃`�����l���X�L�����ȂǂōX�V���Ă��������B
@echo �͂��߂ɔ�j��e�X�g���s���܂��B
@pause
"%~dp0tsidmove.exe" --dry-run
@if errorlevel 1 goto label1

@echo.
@echo �e�X�g�͐���I�����܂����B���ۂɕύX���s���܂��B
@echo �K�v�Ȃ�\��t�@�C�����o�b�N�A�b�v���Ă��������B
@pause
"%~dp0tsidmove.exe" --run
@if errorlevel 1 goto label1
@goto label9

:label1
@echo.
@echo �G���[���������܂����B�I�����܂��B

:label9
@pause
