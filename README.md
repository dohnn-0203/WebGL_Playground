# Merge Cafe Puzzle (머지 카페 퍼즐)

브라우저에서 바로 플레이하는 Unity WebGL 머지 퍼즐 게임입니다.
커피·빵·디저트를 생성하고, 같은 아이템 두 개를 합쳐 상위 메뉴를 만들어 주문을 납품하세요.

## 플레이

- **GitHub Pages**: https://dohnn-0203.github.io/WebGL_Playground/
  (저장소 Settings → Pages → Source를 `main` / `docs`로 설정하면 활성화됩니다)
- **로컬 실행**: 저장소 루트에서 `python -m http.server 8765 --directory docs` 후
  브라우저에서 `http://localhost:8765` 접속

## 게임 방법

1. 좌측 **생성기**(커피머신)를 클릭해 Lv.1 아이템을 만듭니다. 에너지 1이 소모되며 시간이 지나면 회복됩니다.
2. 보드의 아이템을 **드래그**해 이동하고, 같은 종류·같은 레벨 아이템 위에 놓으면 **머지**되어 한 단계 높은 아이템이 됩니다. (각 계열 Lv.1~Lv.5)
3. 우측 **주문** 카드가 요구하는 아이템을 만들면 `납품` 버튼이 켜지고, 납품하면 **골드**를 받습니다.
4. 골드로 하단 패널에서 **보드 확장**(잠긴 칸 해금), **생성기 강화**(최대 에너지↑, Lv.2 생성 확률), 그리고 **오븐(150G)·냉장고(300G) 해금**을 진행합니다.
5. 진행 상황은 행동할 때마다 자동 저장되며(새로고침해도 유지), 상단 `초기화` 버튼으로 처음부터 시작할 수 있습니다.

| 생성기 | 생산 | 에너지 | 회복 | 해금 |
|---|---|---:|---:|---|
| 커피머신 | 커피 계열 | 10 | 30초당 +1 | 기본 |
| 오븐 | 빵 계열 | 8 | 35초당 +1 | 150 골드 |
| 냉장고 | 디저트 계열 | 6 | 45초당 +1 | 300 골드 |

## 개발 정보

- Unity `2022.3.62f3`, 2D 템플릿, 순수 Unity 기능만 사용 (외부 유료/미확인 라이선스 에셋 없음)
- 씬은 `Assets/MergeCafe/Scenes/MergeCafeGame.unity` 하나이며, UI 전체를 코드로 런타임 구성
- 게임 로직은 순수 C# (EditMode 테스트 65건 + PlayMode 통합 테스트 1건)
- 한글 폰트: Noto Sans KR — SIL OFL 1.1
  (고지: `Assets/MergeCafe/Docs/THIRD_PARTY_NOTICES.md`)
- 설계 문서: [`webGL_game.md`](webGL_game.md)

### 빌드

에디터 메뉴 `MergeCafe → Build WebGL (docs)` 또는 배치 모드:

```text
Unity.exe -batchmode -nographics -quit -projectPath <프로젝트 경로> ^
  -buildTarget WebGL -executeMethod MergeCafe.EditorTools.WebGLBuilder.BuildForGitHubPages
```

출력은 저장소 루트 `docs/` (Gzip + Decompression Fallback — GitHub Pages 호환).
