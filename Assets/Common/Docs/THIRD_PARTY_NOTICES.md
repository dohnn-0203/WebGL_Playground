# Third-Party Notices

이 프로젝트는 공개 GitHub 저장소와 GitHub Pages(WebGL) 배포를 전제로 하며,
라이선스가 확인된 리소스만 포함한다. (webGL_game.md §1-2 정책)

## Noto Sans KR (Korean Subset OTF)

- 용도: WebGL 빌드에서 한국어 UI 텍스트 렌더링 (WebGL은 OS 폰트 폴백이 없어 폰트 동봉 필수)
- 파일: `Assets/Common/Resources/Fonts/NotoSansKR-Regular.otf`
- 출처: https://github.com/notofonts/noto-cjk (`Sans/SubsetOTF/KR/NotoSansKR-Regular.otf`)
- 저작권: Copyright 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'
- 라이선스: SIL Open Font License, Version 1.1 (OFL-1.1)
- 라이선스 전문: `Assets/Common/Docs/NotoSansKR-LICENSE-OFL.txt`
- 비고: OFL 1.1은 공개 저장소 커밋, 웹 배포(임베딩)를 허용한다. 폰트 단독 판매만 금지.

## 저장소에서 제외된 로컬 에셋

- `Assets/ithappy/` (Furniture_Cute 팩): 라이선스가 공개 배포용으로 확인되지 않아
  `.gitignore`로 커밋/빌드 대상에서 제외한다. 게임 구현에는 사용하지 않는다.
